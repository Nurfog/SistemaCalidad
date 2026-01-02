using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NoConformidadesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NoConformidadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoConformidad>>> GetNoConformidades()
    {
        return await _context.NoConformidades.Include(nc => nc.Acciones).ToListAsync();
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost]
    public async Task<ActionResult<NoConformidad>> CrearNoConformidad(NoConformidad nc)
    {
        _context.NoConformidades.Add(nc);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetNoConformidades), new { id = nc.Id }, nc);
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPost("{id}/acciones")]
    public async Task<ActionResult<AccionCalidad>> AgregarAccion(int id, AccionCalidad accion)
    {
        var nc = await _context.NoConformidades.FindAsync(id);
        if (nc == null) return NotFound();

        accion.NoConformidadId = id;
        _context.AccionesCalidad.Add(accion);
        await _context.SaveChangesAsync();

        return Ok(accion);
    }
}
