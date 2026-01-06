using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditoriaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Administrador,AuditorInterno,AuditorExterno")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditoriaAcceso>>> GetLogs()
    {
        return await _context.AuditoriaAccesos.OrderByDescending(a => a.Fecha).Take(200).ToListAsync();
    }

    [Authorize(Roles = "Administrador,AuditorInterno,AuditorExterno")]
    [HttpGet("resumen-soluciones")]
    public async Task<IActionResult> GetResumenSoluciones()
    {
        var ncCerradas = await _context.NoConformidades
            .Where(nc => nc.Estado == EstadoNoConformidad.Cerrada)
            .OrderByDescending(nc => nc.FechaCierre)
            .Take(20)
            .Select(nc => new {
                nc.Folio,
                nc.DescripcionHallazgo,
                nc.FechaCierre,
                nc.AnalisisCausa,
                Acciones = nc.Acciones.Select(a => new { a.Descripcion, a.FechaEjecucion })
            })
            .ToListAsync();

        return Ok(ncCerradas);
    }
}
