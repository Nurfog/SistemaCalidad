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
    public class ActualizarEstadoDto
    {
        public EstadoNoConformidad NuevoEstado { get; set; }
        public string? Analisis { get; set; }
    }

    public class VerificarAccionDto
    {
        public bool EsEficaz { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

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
        if (string.IsNullOrWhiteSpace(nc.Folio))
        {
            var anioActual = DateTime.UtcNow.Year;
            var correlativo = await _context.NoConformidades
                .CountAsync(x => x.FechaDeteccion.Year == anioActual) + 1;
            
            nc.Folio = $"NC-{anioActual}-{correlativo:D3}";
        }

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

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoDto dto)
    {
        var nc = await _context.NoConformidades.FindAsync(id);
        if (nc == null) return NotFound();

        nc.Estado = dto.NuevoEstado;
        if (!string.IsNullOrWhiteSpace(dto.Analisis))
        {
            nc.AnalisisCausa = dto.Analisis;
        }

        if (dto.NuevoEstado == EstadoNoConformidad.Cerrada)
        {
            nc.FechaCierre = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Estado actualizado exitosamente", estado = nc.Estado });
    }

    [Authorize(Roles = "Escritor,Administrador")]
    [HttpPatch("acciones/{accionId}/ejecutar")]
    public async Task<IActionResult> EjecutarAccion(int accionId)
    {
        var accion = await _context.AccionesCalidad.FindAsync(accionId);
        if (accion == null) return NotFound();

        accion.FechaEjecucion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Acción marcada como ejecutada", fecha = accion.FechaEjecucion });
    }

    [Authorize(Roles = "Administrador")]
    [HttpPatch("acciones/{accionId}/verificar")]
    public async Task<IActionResult> VerificarAccion(int accionId, [FromBody] VerificarAccionDto dto)
    {
        var accion = await _context.AccionesCalidad.FindAsync(accionId);
        if (accion == null) return NotFound();

        accion.EsEficaz = dto.EsEficaz;
        accion.ObservacionesVerificacion = dto.Observaciones;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Verificación de eficacia registrada", esEficaz = accion.EsEficaz });
    }
}
