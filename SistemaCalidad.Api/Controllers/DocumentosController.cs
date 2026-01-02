using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using SistemaCalidad.Api.Services;

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

    public DocumentosController(ApplicationDbContext context, IFileStorageService fileService, IAuditoriaService auditoria, IEmailService emailService)
    {
        _context = context;
        _fileService = fileService;
        _auditoria = auditoria;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos()
    {
        var query = _context.Documentos.Include(d => d.Revisiones).AsQueryable();

        // Si el usuario es Lector, solo puede ver documentos APROBADOS
        if (User.IsInRole("Lector") && !User.IsInRole("Administrador") && !User.IsInRole("Escritor"))
        {
            query = query.Where(d => d.Estado == EstadoDocumento.Aprobado);
        }

        return await query.ToListAsync();
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost]
    public async Task<ActionResult<Documento>> CrearDocumento([FromForm] string titulo, [FromForm] string codigo, [FromForm] TipoDocumento tipo, [FromForm] AreaProceso area, IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("El archivo es obligatorio.");

        var documento = new Documento
        {
            Titulo = titulo,
            Codigo = codigo,
            Tipo = tipo,
            Area = area,
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
            CreadoPor = "Admin Sistema"
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
            CreadoPor = "Admin Sistema"
        };

        _context.VersionesDocumento.Add(revision);
        await _context.SaveChangesAsync();

        return Ok(documento);
    }

    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarDocumento(int id)
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
        
        await _auditoria.RegistrarAccionAsync("DESCARGA", "Documento", id, $"Descargó {versionVigente.NombreArchivo} (v{versionVigente.NumeroVersion})");
        
        return File(datosArchivo.Content, datosArchivo.ContentType, versionVigente.NombreArchivo);
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
        
        // Notificar a los administradores (ejemplo manual por ahora, se puede automatizar con los IDs)
        try {
            await _emailService.SendEmailAsync("administracion@norteamericano.cl", 
                "SGC: Nueva solicitud de revisión", 
                $"El documento <b>{documento.Titulo}</b> ({documento.Codigo}) ha sido enviado a revisión por {User.Identity?.Name}.");
        } catch { /* Loguear error de correo pero no detener flujo */ }

        return Ok(new { mensaje = "Documento enviado a revisión exitosamente.", estado = documento.Estado });
    }

    [Authorize(Roles = "Administrador")]
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
        }

        await _context.SaveChangesAsync();
        await _auditoria.RegistrarAccionAsync("APROBACION", "Documento", id, "Aprobó el documento formalmente");

        // Notificar al creador o al área pertinente
        try {
            await _emailService.SendEmailAsync("calidad@norteamericano.cl", 
                "SGC: Documento Aprobado", 
                $"El documento <b>{documento.Titulo}</b> ({documento.Codigo}) ha sido aprobado formalmente y ya está disponible para el personal.");
        } catch { /* Loguear error de correo */ }

        return Ok(new { mensaje = "Documento aprobado y publicado.", estado = documento.Estado });
    }
}
