using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CarpetasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CarpetasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Carpetas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarpetaRegistro>>> GetCarpetas()
    {
        return await _context.CarpetasRegistros
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    // POST: api/Carpetas
    [HttpPost]
    [Authorize(Roles = "Administrador,Escritor")]
    public async Task<ActionResult<CarpetaRegistro>> PostCarpeta(CarpetaRegistro carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta.Nombre))
            return BadRequest("El nombre es requerido");

        carpeta.FechaCreacion = DateTime.UtcNow;
        if (string.IsNullOrEmpty(carpeta.Color)) carpeta.Color = "#38bdf8";

        _context.CarpetasRegistros.Add(carpeta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCarpetas), new { id = carpeta.Id }, carpeta);
    }

    // DELETE: api/Carpetas/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DeleteCarpeta(int id)
    {
        var carpeta = await _context.CarpetasRegistros.FindAsync(id);
        if (carpeta == null)
        {
            return NotFound();
        }

        // Verificar si tiene registros asociados
        var tieneRegistros = await _context.RegistrosCalidad.AnyAsync(r => r.CarpetaRegistroId == id && !r.EstaEliminado);
        if (tieneRegistros)
        {
            return BadRequest("No se puede eliminar la carpeta porque contiene registros. Vacíela primero.");
        }

        _context.CarpetasRegistros.Remove(carpeta);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
