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

    public AnexosController(ApplicationDbContext context, IFileStorageService fileService, IAuditoriaService auditoria)
    {
        _context = context;
        _fileService = fileService;
        _auditoria = auditoria;
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

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Plantillas");

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
}
