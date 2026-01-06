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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos(
        [FromQuery] string? buscar, 
        [FromQuery] TipoDocumento? tipo, 
        [FromQuery] AreaProceso? area, 
        [FromQuery] EstadoDocumento? estado,
        [FromQuery] int? carpetaId) // Filtro por carpeta
    {
        var query = _context.Documentos.Include(d => d.Revisiones).AsQueryable();

        // 0. Filtro por Carpeta (Si se envía null, se asume raíz o todos?
        // Usualmente para navegar: si carpetaId es null, traer docs sin carpeta (o de raiz)
        // Pero para búsquedas globales se podría ignorar.
        // Haremos lógica de navegación: Si no hay termino de busqueda 'buscar', filtramos por carpeta.
        if (string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(d => d.CarpetaDocumentoId == carpetaId);
        }

        // 1. Busqueda por texto (Código o Título)
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(d => d.Codigo.Contains(buscar) || d.Titulo.Contains(buscar));
        }

        // 2. Filtros específicos
        if (tipo.HasValue) query = query.Where(d => d.Tipo == tipo.Value);
        if (area.HasValue) query = query.Where(d => d.Area == area.Value);
        if (estado.HasValue) query = query.Where(d => d.Estado == estado.Value);

        // 3. Seguridad Granular por Roles
        var isAdminOrAuditor = User.IsInRole("Administrador") || User.IsInRole("AuditorInterno") || User.IsInRole("AuditorExterno");
        var isResponsable = User.IsInRole("Responsable");

        if (!isAdminOrAuditor && !isResponsable)
        {
            // Usuarios Lector/Común solo ven Aprobados
            query = query.Where(d => d.Estado == EstadoDocumento.Aprobado);
        }

        return await query.ToListAsync();
    }

    [Authorize(Roles = "Administrador,Escritor,Responsable")]
    [HttpPost]
    public async Task<ActionResult<Documento>> CrearDocumento(
        [FromForm] string titulo, 
        [FromForm] string codigo, 
        [FromForm] TipoDocumento tipo, 
        [FromForm] AreaProceso area, 
        [FromForm] int? carpetaId,
        IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("El archivo es obligatorio.");

        var documento = new Documento
        {
            Titulo = titulo,
            Codigo = codigo,
            Tipo = tipo,
            Area = area,
            CarpetaDocumentoId = carpetaId, // Asignar carpeta
            Estado = EstadoDocumento.Borrador,
            VersionActual = 1
        };

        _context.Documentos.Add(documento);
        await _context.SaveChangesAsync();

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Documentos");

        var revision = new VersionDocumento
        {
            DocumentoId = documento.Id,
            NumeroVersion = 1,
            DescripcionCambio = "Creación inicial",
            NombreArchivo = archivo.FileName,
            RutaArchivo = rutaArchivo,
            TipoContenido = archivo.ContentType,
            EsVersionActual = true,
            CreadoPor = User.Identity?.Name ?? "Sistema",
            EstadoRevision = "Pendiente" // Se crea como pendiente para que el auditor lo vea
        };

        _context.VersionesDocumento.Add(revision);
        await _context.SaveChangesAsync();

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
            var documento = await _context.Documentos.Include(d => d.Revisiones).FirstOrDefaultAsync(d => d.Id == id);
            if (documento == null) return NotFound();

            var versionVigente = documento.Revisiones.FirstOrDefault(r => r.EsVersionActual);
            if (versionVigente == null) return NotFound("No se encontró una versión activa para analizar.");

            // 1. Obtener el archivo físico
            var datosArchivo = await _fileService.GetFileAsync(versionVigente.RutaArchivo);
            
            // 2. Copiar a memoria
            byte[] contenido;
            using (var ms = new MemoryStream())
            {
                await datosArchivo.Content.CopyToAsync(ms);
                contenido = ms.ToArray();
            }
            
            // 3. Convertir a PDF si es necesario (igual que en DescargarDocumento)
            string extension = Path.GetExtension(versionVigente.NombreArchivo).ToLower();
            
            if (extension != ".pdf")
            {
                Console.WriteLine($"[ChatDocumento] Convirtiendo {extension} a PDF...");
                try
                {
                    contenido = _converterService.ConvertToPdf(contenido, extension);
                    Console.WriteLine($"[ChatDocumento] Conversión exitosa. Nuevo tamaño: {contenido.Length} bytes");
                }
                catch (Exception convEx)
                {
                    Console.WriteLine($"[ChatDocumento] Error en conversión: {convEx.Message}");
                    return BadRequest($"No se pudo convertir el documento {extension} a PDF: {convEx.Message}");
                }
            }
            
            // 4. Extraer texto plano con iText7
            var sb = new StringBuilder();
            using (var ms = new MemoryStream(contenido))
            {
                using (var pdfReader = new PdfReader(ms))
                using (var pdfDoc = new PdfDocument(pdfReader))
                {
                    var pages = pdfDoc.GetNumberOfPages();
                    for (int i = 1; i <= pages; i++)
                    {
                        var page = pdfDoc.GetPage(i);
                        var text = PdfTextExtractor.GetTextFromPage(page);
                        sb.AppendLine(text);
                    }
                }
            }

            string contenidoTexto = sb.ToString();

            if (string.IsNullOrWhiteSpace(contenidoTexto))
                return BadRequest("El documento no contiene texto legible (quizás es una imagen escaneada).");

            // 3. Consultar a la IA
            var respuesta = await _iaService.GenerarRespuesta(request.Pregunta, contenidoTexto);

            await _auditoria.RegistrarAccionAsync("CONSULTA_IA", "Documento", id, $"Pregunta: {request.Pregunta}");

            return Ok(new { respuesta });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentosController] Error en Chat IA: {ex}");
            Console.WriteLine($"[DocumentosController] Stack Trace: {ex.StackTrace}");
            
            // Mensaje más específico según el tipo de error
            string mensajeUsuario = ex.Message.Contains("archivo") || ex.Message.Contains("file") 
                ? "El archivo del documento no está disponible en el servidor. Por favor, contacta al administrador."
                : ex.Message.Contains("API") || ex.Message.Contains("Google")
                ? "Error al comunicarse con el servicio de IA. Verifica la configuración de la API Key."
                : $"Error al procesar consulta con IA: {ex.Message}";
                
            return StatusCode(500, mensajeUsuario);
        }
    }
}

public class ChatRequest
{
    public string Pregunta { get; set; }
}

public class MoverDocumentoRequest
{
    public int? CarpetaId { get; set; }
}
