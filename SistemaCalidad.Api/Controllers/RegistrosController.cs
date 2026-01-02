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
    public async Task<ActionResult<IEnumerable<RegistroCalidad>>> GetRegistros([FromQuery] string? buscar)
    {
        var query = _context.RegistrosCalidad.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(r => r.Nombre.Contains(buscar) || r.Identificador.Contains(buscar));
        }

        return await query.ToListAsync();
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost]
    public async Task<ActionResult<RegistroCalidad>> CrearRegistro([FromForm] string nombre, [FromForm] string identificador, [FromForm] int anosRetencion, IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("El archivo es obligatorio para la evidencia.");

        var rutaArchivo = await _fileService.SaveFileAsync(archivo.OpenReadStream(), archivo.FileName, "Registros");

        var registro = new RegistroCalidad
        {
            Nombre = nombre,
            Identificador = identificador,
            AnosRetencion = anosRetencion,
            RutaArchivo = rutaArchivo,
            FechaAlmacenamiento = DateTime.UtcNow
        };

        _context.RegistrosCalidad.Add(registro);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRegistros), new { id = registro.Id }, registro);
    }
}
