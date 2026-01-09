using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SistemaCalidad.Api.Hubs;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using SistemaCalidad.Api.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileService;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmailService _emailService;
    private readonly IWatermarkService _watermarkService;
    private readonly IHubContext<NotificacionHub> _hubContext;
    private readonly IDocumentConverterService _converterService;
    private readonly IIAService _iaService;

    public DocumentosController(ApplicationDbContext context, IFileStorageService fileService, IAuditoriaService auditoria, IEmailService emailService, IWatermarkService watermarkService, IHubContext<NotificacionHub> hubContext, IDocumentConverterService converterService, IIAService iaService)
    {
        _context = context;
        _fileService = fileService;
        _auditoria = auditoria;
        _emailService = emailService;
        _watermarkService = watermarkService;
        _hubContext = hubContext;
        _converterService = converterService;
        _iaService = iaService;
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("reset-total")]
    public async Task<IActionResult> ResetTotal()
    {
        try
        {
            Console.WriteLine("[Purga] Iniciando proceso de eliminación total...");

            // 1. Obtener todas las versiones de documentos para borrar archivos físicos
            var versiones = await _context.VersionesDocumento.ToListAsync();
            int archivosBorrados = 0;

            foreach (var version in versiones)
            {
                if (!string.IsNullOrEmpty(version.RutaArchivo))
                {
                    try
                    {
                        await _fileService.DeleteFileAsync(version.RutaArchivo);
                        archivosBorrados++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Purga] Error al borrar archivo S3 {version.RutaArchivo}: {ex.Message}");
                    }
                }
            }

            // 2. Limpiar Base de Datos - Documentos
            _context.VersionesDocumento.RemoveRange(versiones);
            var documentos = await _context.Documentos.ToListAsync();
            _context.Documentos.RemoveRange(documentos);

            await _context.SaveChangesAsync();

            // 3. Limpiar Carpetas (después de los documentos para evitar FK constraint)
            var carpetas = await _context.CarpetasDocumentos.ToListAsync();
            _context.CarpetasDocumentos.RemoveRange(carpetas);
            
            await _context.SaveChangesAsync();

            // 4. Registrar en Auditoría
            await _auditoria.RegistrarAccionAsync("PURGA_TOTAL", "Sistema", 0, 
                $"Se eliminaron {documentos.Count} documentos, {archivosBorrados} archivos físicos y {carpetas.Count} carpetas.");

            // 5. Sincronizar IA (para que limpie su KB)
            try
            {
                _ = _iaService.SincronizarS3Async();
            }
            catch { }

            return Ok(new 
            { 
                mensaje = "Purga de sistema completada con éxito.", 
                documentosEliminados = documentos.Count,
                archivosFisicosEliminados = archivosBorrados,
                carpetasEliminadas = carpetas.Count
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Purga] Error crítico: {ex.Message}");
            return StatusCode(500, $"Error durante la purga: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentoDto>>> GetDocumentos(
        [FromQuery] string? buscar, 
        [FromQuery] TipoDocumento? tipo, 
        [FromQuery] AreaProceso? area, 
        [FromQuery] EstadoDocumento? estado,
        [FromQuery] int? carpetaId)
    {
        var query = _context.Documentos.AsNoTracking().AsQueryable();

        // 0. Filtro por Carpeta
        if (string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(d => d.CarpetaDocumentoId == carpetaId);
        }

        // 1. Busqueda por texto
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(d => d.Codigo.Contains(buscar) || d.Titulo.Contains(buscar));
        }

        // 2. Filtros específicos
        if (tipo.HasValue) query = query.Where(d => d.Tipo == tipo.Value);
        if (area.HasValue) query = query.Where(d => d.Area == area.Value);
        if (estado.HasValue) query = query.Where(d => d.Estado == estado.Value);

        // 3. Seguridad Granular
        var isAdminOrAuditor = User.IsInRole("Administrador") || User.IsInRole("AuditorInterno") || User.IsInRole("AuditorExterno");
        var isResponsable = User.IsInRole("Responsable");

        if (!isAdminOrAuditor && !isResponsable)
        {
            query = query.Where(d => d.Estado == EstadoDocumento.Aprobado);
        }

        // 4. Proyección Optimizada (Evita traer todas las revisiones)
        var result = await query.Select(d => new DocumentoDto
        {
            Id = d.Id,
            Codigo = d.Codigo,
            Titulo = d.Titulo,
            Tipo = d.Tipo,
            Area = d.Area,
            Estado = d.Estado,
            VersionActual = d.VersionActual,
            FechaActualizacion = d.FechaActualizacion ?? d.FechaCreacion,
            // Obtener solo el nombre del archivo de la versión actual
            NombreArchivoActual = d.Revisiones
                .Where(r => r.EsVersionActual)
                .Select(r => r.NombreArchivo)
                .FirstOrDefault()
        }).ToListAsync();

        return Ok(result);
    }

    [Authorize(Roles = "Administrador,Escritor,Responsable")]
    [HttpPost]
    public async Task<ActionResult<Documento>> CrearDocumento(
        [FromForm] string titulo, 
        [FromForm] string codigo, 
        [FromForm] TipoDocumento tipo, 
        [FromForm] AreaProceso area, 
        [FromForm] int? carpetaId,
        [FromForm] int? numeroRevision, // Nuevo parámetro opcional
        IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("El archivo es obligatorio.");

        int revisionInicial = numeroRevision ?? 1;

        var documento = new Documento
        {
            Titulo = titulo,
            Codigo = codigo,
            Tipo = tipo,
            Area = area,
            CarpetaDocumentoId = carpetaId,
            Estado = EstadoDocumento.Borrador,
            VersionActual = revisionInicial
        };

        _context.Documentos.Add(documento);
        await _context.SaveChangesAsync();

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Documentos");

        var revision = new VersionDocumento
        {
            DocumentoId = documento.Id,
            NumeroVersion = revisionInicial,
            DescripcionCambio = revisionInicial == 1 ? "Creación inicial" : $"Carga inicial (Migración Rev {revisionInicial})",
            NombreArchivo = archivo.FileName,
            RutaArchivo = rutaArchivo,
            TipoContenido = archivo.ContentType,
            EsVersionActual = true,
            CreadoPor = User.Identity?.Name ?? "Sistema",
            EstadoRevision = "Pendiente"
        };

        _context.VersionesDocumento.Add(revision);
        await _context.SaveChangesAsync();

        // Disparar sincronización con IA (Base de Conocimiento)
        _ = _iaService.SincronizarS3Async();

        return CreatedAtAction(nameof(GetDocumentos), new { id = documento.Id }, documento);
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost("{id}/revision")]
    public async Task<IActionResult> AgregarRevision(int id, [FromForm] string descripcionCambio, IFormFile archivo)
    {
        var documento = await _context.Documentos.Include(d => d.Revisiones).FirstOrDefaultAsync(d => d.Id == id);
        if (documento == null) return NotFound();

        var actual = documento.Revisiones.FirstOrDefault(r => r.EsVersionActual);
        if (actual != null) actual.EsVersionActual = false;

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Documentos");
        
        documento.VersionActual++;
        documento.FechaActualizacion = DateTime.UtcNow;

        var revision = new VersionDocumento
        {
            DocumentoId = documento.Id,
            NumeroVersion = documento.VersionActual,
            DescripcionCambio = descripcionCambio,
            NombreArchivo = archivo.FileName,
            RutaArchivo = rutaArchivo,
            TipoContenido = archivo.ContentType,
            EsVersionActual = true,
            CreadoPor = User.Identity?.Name ?? "Sistema",
            EstadoRevision = "Pendiente"
        };

        _context.VersionesDocumento.Add(revision);
        await _context.SaveChangesAsync();

        // Disparar sincronización con IA (Base de Conocimiento)
        _ = _iaService.SincronizarS3Async();

        return Ok(documento);
    }

    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarDocumento(int id)
    {
        try 
        {
            var documento = await _context.Documentos.Include(d => d.Revisiones).FirstOrDefaultAsync(d => d.Id == id);
            if (documento == null) return NotFound();

            // Seguridad: Un Lector no puede bajar borradores
            if (User.IsInRole("Lector") && !User.IsInRole("Administrador") && documento.Estado != EstadoDocumento.Aprobado)
            {
                return Forbid("No tienes permiso para descargar documentos en estado borrador o revisión.");
            }

            var versionVigente = documento.Revisiones.FirstOrDefault(r => r.EsVersionActual);
            if (versionVigente == null) return NotFound("No se encontró una versión activa.");

            var datosArchivo = await _fileService.GetFileAsync(versionVigente.RutaArchivo);
            byte[] contenido;
            using (var ms = new MemoryStream())
            {
                await datosArchivo.Content.CopyToAsync(ms);
                contenido = ms.ToArray();
            }

            // Conversión y Marca de Agua
            string extension = Path.GetExtension(versionVigente.NombreArchivo).ToLower();
            string nombreDescarga = versionVigente.NombreArchivo;
            string contentTypeDescarga = datosArchivo.ContentType;

            if (extension == ".docx" || extension == ".txt" || extension == ".pdf")
            {
                try 
                {
                    // 1. Convertir a PDF si es necesario
                    if (extension != ".pdf")
                    {
                        contenido = _converterService.ConvertToPdf(contenido, extension);
                        nombreDescarga = Path.ChangeExtension(nombreDescarga, ".pdf");
                        contentTypeDescarga = "application/pdf";
                    }

                    // 2. Aplicar Marca de Agua (Siempre, ya que ahora es PDF)
                    contenido = _watermarkService.ApplyWatermark(contenido, User.Identity?.Name ?? "Usuario Anónimo", $"Documento: {documento.Codigo}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DocumentosController] Error CRITICO en procesamiento PDF: {ex}");
                    // En este punto, si falla la seguridad (conversión/watermark), NO DEBEMOS entregar el archivo original.
                    // Esto cumple el requisito "para que la opcion de seguridad se cumpla".
                    return StatusCode(500, $"Error al procesar la seguridad del documento: {ex.Message}");
                }
            }
            
            await _auditoria.RegistrarAccionAsync("DESCARGA", "Documento", id, $"Descargó {nombreDescarga} (v{versionVigente.NumeroVersion})");
            
            return File(contenido, contentTypeDescarga, nombreDescarga);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentosController] Error descargando documento {id}: {ex.Message}");
            return StatusCode(500, $"Error interno al descargar: {ex.Message}");
        }
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost("{id}/solicitar-revision")]
    public async Task<IActionResult> SolicitarRevision(int id)
    {
        var documento = await _context.Documentos.FindAsync(id);
        if (documento == null) return NotFound();

        if (documento.Estado != EstadoDocumento.Borrador)
            return BadRequest("Solo se pueden enviar a revisión documentos en estado Borrador.");

        documento.Estado = EstadoDocumento.EnRevision;
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAccionAsync("SOLICITUD_REVISION", "Documento", id, "Envió documento a revisión");
        
        // Notificación en tiempo real
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", User.Identity?.Name, $"Nueva solicitud de revisión para: {documento.Titulo}");
        
        // Notificar a los administradores (ejemplo manual por ahora, se puede automatizar con los IDs)
        try {
            await _emailService.SendEmailAsync("administracion@norteamericano.cl", 
                "SGC: Nueva solicitud de revisión", 
                $"El documento <b>{documento.Titulo}</b> ({documento.Codigo}) ha sido enviado a revisión por {User.Identity?.Name}.");
        } catch { /* Loguear error de correo pero no detener flujo */ }

        return Ok(new { mensaje = "Documento enviado a revisión exitosamente.", estado = documento.Estado });
    }

    [Authorize(Roles = "Administrador,AuditorInterno")]
    [HttpPost("{id}/aprobar")]
    public async Task<IActionResult> AprobarDocumento(int id)
    {
        var documento = await _context.Documentos.Include(d => d.Revisiones).FirstOrDefaultAsync(d => d.Id == id);
        if (documento == null) return NotFound();

        if (documento.Estado != EstadoDocumento.EnRevision)
            return BadRequest("Solo se pueden aprobar documentos que estén En Revisión.");

        documento.Estado = EstadoDocumento.Aprobado;
        documento.FechaActualizacion = DateTime.UtcNow;

        var versionActual = documento.Revisiones.FirstOrDefault(r => r.EsVersionActual);
        if (versionActual != null)
        {
            versionActual.AprobadoPor = User.Identity?.Name ?? "Admin";
            versionActual.FechaAprobacion = DateTime.UtcNow;
            versionActual.EstadoRevision = "Aprobado";
            versionActual.FechaRevision = DateTime.UtcNow;
            versionActual.RevisadoPorId = User.Identity?.Name;
        }

        await _context.SaveChangesAsync();
        await _auditoria.RegistrarAccionAsync("APROBACION", "Documento", id, "Aprobó el documento formalmente");

        // Notificación en tiempo real
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Admin", $"Documento aprobado: {documento.Titulo}");

        // Notificar al creador o al área pertinente
        try {
            await _emailService.SendEmailAsync("calidad@norteamericano.cl", 
                "SGC: Documento Aprobado", 
                $"El documento <b>{documento.Titulo}</b> ({documento.Codigo}) ha sido aprobado formalmente y ya está disponible para el personal.");
        } catch { /* Loguear error de correo */ }

        return Ok(new { mensaje = "Documento aprobado y publicado.", estado = documento.Estado });
    }

    [Authorize(Roles = "Administrador,AuditorInterno")]
    [HttpPost("{id}/rechazar")]
    public async Task<IActionResult> RechazarDocumento(int id, [FromForm] string observaciones)
    {
        var documento = await _context.Documentos.FindAsync(id);
        if (documento == null) return NotFound();

        if (documento.Estado != EstadoDocumento.EnRevision)
            return BadRequest("Solo se pueden rechazar documentos que estén En Revisión.");

        var versionActual = await _context.VersionesDocumento.FirstOrDefaultAsync(v => v.DocumentoId == id && v.EsVersionActual);
        if (versionActual != null)
        {
            versionActual.EstadoRevision = "Rechazado";
            versionActual.ObservacionesRevision = observaciones;
            versionActual.FechaRevision = DateTime.UtcNow;
            versionActual.RevisadoPorId = User.Identity?.Name;
        }

        await _context.SaveChangesAsync();
        await _auditoria.RegistrarAccionAsync("RECHAZO", "Documento", id, $"Rechazó el documento. Obs: {observaciones}");

        // Notificación en tiempo real
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Admin", $"Documento rechazado: {documento.Titulo}. Observación: {observaciones}");

        try {
            await _emailService.SendEmailAsync("calidad@norteamericano.cl", 
                "SGC: Documento Rechazado", 
                $"El documento <b>{documento.Titulo}</b> ha sido devuelto a corregir. <br/><b>Observaciones:</b> {observaciones}");
        } catch { }

        return Ok(new { mensaje = "Documento devuelto a borrador.", estado = documento.Estado });
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPut("{id}/mover")]
    public async Task<IActionResult> MoverDocumento(int id, [FromBody] MoverDocumentoRequest request)
    {
        var documento = await _context.Documentos.FindAsync(id);
        if (documento == null) return NotFound();

        // Validar que la carpeta exista si no es null (raíz)
        if (request.CarpetaId.HasValue)
        {
            var existeCarpeta = await _context.CarpetasDocumentos.AnyAsync(c => c.Id == request.CarpetaId);
            if (!existeCarpeta) return BadRequest("La carpeta destino no existe.");
        }

        documento.CarpetaDocumentoId = request.CarpetaId;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Documento movido exitosamente." });
    }

    [Authorize]
    [HttpPost("{id}/chat")]
    public async Task<IActionResult> ChatDocumento(int id, [FromBody] ChatRequest request)
    {
        try
        {
            var documento = await _context.Documentos.FirstOrDefaultAsync(d => d.Id == id);
            if (documento == null) return NotFound();

            // 1. Consultar a la IA usando RAG (Knowledge Base)
            var usuarioAi = User.Identity?.Name ?? "anonimo";
            
            // Ya no procesamos el PDF aquí, la IA lo tiene en su KB (S3 Sync)
            var respuesta = await _iaService.GenerarRespuesta(request.Pregunta, null, usuarioAi);

            await _auditoria.RegistrarAccionAsync("CONSULTA_IA", "Documento", id, $"Pregunta: {request.Pregunta}");

            return Ok(new { respuesta });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentosController] Error en Chat IA: {ex}");
            return StatusCode(500, $"Error al procesar consulta con IA: {ex.Message}");
        }
    }

    [Authorize(Roles = "Administrador,Escritor,Responsable")]
    [HttpPost("redactar")]
    public async Task<IActionResult> RedactarDocumento([FromBody] RedactarDocumentoRequest request)
    {
        try
        {
            Documento documento;
            int version;
            string descripcionCambio = request.DescripcionCambio ?? "Creación desde redactor";

            if (request.Id.HasValue)
            {
                // ACTUALIZACIÓN DE DOCUMENTO EXISTENTE
                documento = await _context.Documentos.Include(d => d.Revisiones).FirstOrDefaultAsync(d => d.Id == request.Id.Value);
                if (documento == null) return NotFound("Documento base no encontrado.");

                // Desactivar versión anterior
                var actual = documento.Revisiones.FirstOrDefault(r => r.EsVersionActual);
                if (actual != null)
                {
                    actual.EsVersionActual = false;
                    // También podríamos marcar el documento como 'En Revisión' si se desea flujo de aprobación
                }

                documento.VersionActual++;
                documento.FechaActualizacion = DateTime.UtcNow;
                documento.Estado = EstadoDocumento.Borrador; // Vuelve a borrador para revisión
                version = documento.VersionActual;
            }
            else
            {
                // NUEVO DOCUMENTO
                documento = new Documento
                {
                    Titulo = request.Titulo,
                    Codigo = request.Codigo,
                    Tipo = request.Tipo,
                    Area = request.Area,
                    CarpetaDocumentoId = request.CarpetaId,
                    Estado = EstadoDocumento.Borrador,
                    VersionActual = 1,
                    FechaCreacion = DateTime.UtcNow
                };
                _context.Documentos.Add(documento);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID
                version = 1;
            }

            // 1. Leer Plantilla Maestro
            var masterPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "MasterTemplate.html");
            if (!System.IO.File.Exists(masterPath)) return StatusCode(500, "La plantilla maestro no existe en el servidor.");

            var masterHtml = await System.IO.File.ReadAllTextAsync(masterPath);

            // 2. Inyectar datos
            var finalHtml = masterHtml
                .Replace("{{TITULO}}", documento.Titulo)
                .Replace("{{CODIGO}}", documento.Codigo)
                .Replace("{{VERSION}}", version.ToString())
                .Replace("{{FECHA}}", DateTime.Now.ToString("dd/MM/yyyy"))
                .Replace("{{CONTENIDO}}", request.ContenidoHtml);

            // 3. Generar PDF
            byte[] pdfBytes;
            using (var ms = new MemoryStream())
            {
                iText.Html2pdf.HtmlConverter.ConvertToPdf(finalHtml, ms);
                pdfBytes = ms.ToArray();
            }

            // 4. Guardar archivo en S3/Local
            var nombreArchivo = $"{documento.Codigo}_v{version}.pdf";
            using (var pdfStream = new MemoryStream(pdfBytes))
            {
                var rutaArchivo = await _fileService.SaveFileAsync(pdfStream, nombreArchivo, "Documentos");

                // 5. Crear nueva versión
                var revision = new VersionDocumento
                {
                    DocumentoId = documento.Id,
                    NumeroVersion = version,
                    DescripcionCambio = descripcionCambio,
                    NombreArchivo = nombreArchivo,
                    RutaArchivo = rutaArchivo,
                    TipoContenido = "application/pdf",
                    EsVersionActual = true,
                    CreadoPor = User.Identity?.Name ?? "Sistema",
                    EstadoRevision = "Pendiente"
                };

                _context.VersionesDocumento.Add(revision);
            }

            await _context.SaveChangesAsync();
            await _auditoria.RegistrarAccionAsync("REDACTAR_DOCUMENTO", "Documento", documento.Id, $"Redactó versión {version} de: {documento.Titulo}");

            // Disparar sincronización con IA (Base de Conocimiento)
            _ = _iaService.SincronizarS3Async();

            return Ok(documento);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentosController] Error al redactar documento: {ex.Message}");
            return StatusCode(500, $"Error al procesar el documento: {ex.Message}");
        }
    }
}

public class RedactarDocumentoRequest
{
    public int? Id { get; set; } // Opcional para actualizaciones
    public string Titulo { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public TipoDocumento Tipo { get; set; }
    public AreaProceso Area { get; set; }
    public int? CarpetaId { get; set; }
    public string ContenidoHtml { get; set; } = string.Empty;
    public string? DescripcionCambio { get; set; }
}

public class ChatRequest
{
    public string Pregunta { get; set; }
}

public class MoverDocumentoRequest
{
    public int? CarpetaId { get; set; }
}
