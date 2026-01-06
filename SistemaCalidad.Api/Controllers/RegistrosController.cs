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
public class RegistrosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileService;

    public RegistrosController(ApplicationDbContext context, IFileStorageService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegistroCalidad>>> GetRegistros([FromQuery] string? buscar, [FromQuery] int? carpetaId)
    {
        var query = _context.RegistrosCalidad
            .Include(r => r.Carpeta)
            .Where(r => !r.EstaEliminado)
            .AsQueryable();

        // Filtrar por carpeta (null = raíz)
        if (carpetaId.HasValue)
        {
            query = query.Where(r => r.CarpetaRegistroId == carpetaId.Value);
        }
        else if (carpetaId == null) // Explícitamente raíz
        {
            query = query.Where(r => r.CarpetaRegistroId == null);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(r => r.Nombre.Contains(buscar) || r.Identificador.Contains(buscar));
        }

        return await query.OrderByDescending(r => r.FechaAlmacenamiento).ToListAsync();
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost]
    public async Task<ActionResult<RegistroCalidad>> CrearRegistro(
        [FromForm] string nombre, 
        [FromForm] string identificador, 
        [FromForm] int anosRetencion, 
        [FromForm] int? carpetaId,
        IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("El archivo es obligatorio para la evidencia.");

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Registros");

        var registro = new RegistroCalidad
        {
            Nombre = nombre,
            Identificador = identificador,
            AnosRetencion = anosRetencion,
            RutaArchivo = rutaArchivo,
            FechaAlmacenamiento = DateTime.UtcNow,
            CarpetaRegistroId = carpetaId
        };

        _context.RegistrosCalidad.Add(registro);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRegistros), new { id = registro.Id }, registro);
    }

    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarRegistro(int id)
    {
        var registro = await _context.RegistrosCalidad.FindAsync(id);
        if (registro == null) return NotFound();

        if (string.IsNullOrEmpty(registro.RutaArchivo)) return NotFound("No hay archivo asociado a este registro.");

        var datosArchivo = await _fileService.GetFileAsync(registro.RutaArchivo);
        
        // El nombre original suele estar despues del primer guion bajo si se usa Guid_OriginalName
        var originalName = registro.RutaArchivo.Contains("_") 
            ? registro.RutaArchivo.Substring(registro.RutaArchivo.IndexOf('_') + 1)
            : Path.GetFileName(registro.RutaArchivo);

        return File(datosArchivo.Content, datosArchivo.ContentType, originalName);
    }
}
