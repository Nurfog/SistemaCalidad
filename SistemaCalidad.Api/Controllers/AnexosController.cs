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
public class AnexosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileService;
    private readonly IAuditoriaService _auditoria;
    private readonly TemplateService _templateService;

    public AnexosController(ApplicationDbContext context, IFileStorageService fileService, IAuditoriaService auditoria, TemplateService templateService)
    {
        _context = context;
        _fileService = fileService;
        _auditoria = auditoria;
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Anexo>>> GetAnexos([FromQuery] string? buscar)
    {
        var query = _context.Anexos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(a => a.Nombre.Contains(buscar) || a.Codigo.Contains(buscar));
        }

        return await query.ToListAsync();
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost]
    public async Task<ActionResult<Anexo>> CrearAnexo([FromForm] string nombre, [FromForm] string codigo, [FromForm] string? descripcion, [FromForm] bool esObligatorio, IFormFile archivo)
    {
        if (archivo == null) return BadRequest("El archivo de la plantilla es obligatorio.");

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Templates");

        var anexo = new Anexo
        {
            Nombre = nombre,
            Codigo = codigo,
            Descripcion = descripcion,
            EsObligatorio = esObligatorio,
            RutaArchivo = rutaArchivo,
            Formato = System.IO.Path.GetExtension(archivo.FileName).Replace(".", "").ToUpper(),
            FechaPublicacion = DateTime.UtcNow
        };

        _context.Anexos.Add(anexo);
        await _context.SaveChangesAsync();

        await _auditoria.RegistrarAccionAsync("CREACION_PLANTILLA", "Anexo", anexo.Id, $"Cargó plantilla: {anexo.Nombre}");

        return CreatedAtAction(nameof(GetAnexos), new { id = anexo.Id }, anexo);
    }

    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarAnexo(int id)
    {
        try 
        {
            var anexo = await _context.Anexos.FindAsync(id);
            if (anexo == null) return NotFound("Anexo no encontrado en la base de datos.");

            var datosArchivo = await _fileService.GetFileAsync(anexo.RutaArchivo);
            
            await _auditoria.RegistrarAccionAsync("DESCARGA_PLANTILLA", "Anexo", id, $"Descargó plantilla: {anexo.Nombre}");
            
            return File(datosArchivo.Content, datosArchivo.ContentType, System.IO.Path.GetFileName(anexo.RutaArchivo));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnexosController] Error descargando anexo {id}: {ex.Message}");
            return StatusCode(500, $"Error interno al descargar: {ex.Message}");
        }
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnexo(int id)
    {
        try
        {
            var anexo = await _context.Anexos.FindAsync(id);
            if (anexo == null) return NotFound("Anexo no encontrado.");

            // 1. Intentar borrar de S3 si existe la ruta
            if (!string.IsNullOrEmpty(anexo.RutaArchivo))
            {
                try {
                    await _fileService.DeleteFileAsync(anexo.RutaArchivo);
                } catch (Exception s3Ex) {
                    Console.WriteLine($"[AnexosController] Error al borrar de S3 ({anexo.RutaArchivo}): {s3Ex.Message}");
                    // Continuamos aunque falle S3 para permitir limpiar la DB si el archivo ya no esta
                }
            }

            // 2. Borrar de la base de datos
            _context.Anexos.Remove(anexo);
            await _context.SaveChangesAsync();

            // 3. Auditar
            await _auditoria.RegistrarAccionAsync("ELIMINAR_PLANTILLA", "Anexo", id, $"Eliminó plantilla: {anexo.Nombre}");

            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnexosController] Error al eliminar anexo {id}: {ex.Message}");
            return StatusCode(500, $"Error interno al eliminar: {ex.Message}");
        }
    }

    [HttpGet("{id}/tags")]
    public async Task<IActionResult> ScanTags(int id)
    {
        var anexo = await _context.Anexos.FindAsync(id);
        if (anexo == null) return NotFound("Anexo no encontrado.");

        if (anexo.Formato.ToUpper() != "DOCX")
            return BadRequest("Solo se pueden escanear etiquetas en archivos DOCX.");

        var datosArchivo = await _fileService.GetFileAsync(anexo.RutaArchivo);
        using var memStream = new MemoryStream();
        await datosArchivo.Content.CopyToAsync(memStream);
        memStream.Position = 0;

        var tags = _templateService.ExtractTags(memStream);
        return Ok(tags);
    }

    [HttpPost("{id}/generar")]
    public async Task<IActionResult> GenerarDocumento(int id, [FromBody] Dictionary<string, string> valores)
    {
        try
        {
            var anexo = await _context.Anexos.FindAsync(id);
            if (anexo == null) return NotFound("Anexo no encontrado.");

            if (anexo.Formato.ToUpper() != "DOCX")
                return BadRequest("La generación automática solo está disponible para plantillas DOCX.");

            var datosArchivo = await _fileService.GetFileAsync(anexo.RutaArchivo);
            using var templateStream = new MemoryStream();
            await datosArchivo.Content.CopyToAsync(templateStream);
            
            var generatedBytes = _templateService.GenerateDocument(templateStream.ToArray(), valores);
            
            await _auditoria.RegistrarAccionAsync("GENERAR_DOCUMENTO", "Anexo", id, $"Generó documento desde plantilla: {anexo.Nombre}");

            var nombreFinal = $"{anexo.Nombre}_Generado_{DateTime.Now:yyyyMMdd_HHmm}.docx";
            return File(generatedBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", nombreFinal);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al generar documento: {ex.Message}");
        }
    }
}
