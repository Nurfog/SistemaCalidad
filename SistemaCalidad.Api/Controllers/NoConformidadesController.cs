using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        var ncs = await _context.NoConformidades.Include(nc => nc.Acciones).ToListAsync();
        
        // Obtener el ID del usuario actual (RUT)
        var currentUserId = User.Identity?.Name;
        
        // Verificación robusta de roles
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isAdminOrAuditor = roles.Any(r => 
            r.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || 
            r.Equals("Escritor", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("AuditorInterno", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("AuditorExterno", StringComparison.OrdinalIgnoreCase));

        if (!isAdminOrAuditor && !string.IsNullOrEmpty(currentUserId))
        {
            // Solo ver NCs donde soy el creador O tengo acciones asignadas
            // Y que aún no estén verificadas/cerradas (Estado < 3)
            ncs = ncs.Where(nc => 
                (nc.CreadoPorId == currentUserId || nc.Acciones.Any(a => a.Responsable == currentUserId)) &&
                nc.Estado < EstadoNoConformidad.Verificada
            ).ToList();

            // Dentro de las NCs visibles, el responsable solo ve SUS acciones
            foreach(var nc in ncs)
            {
                nc.Acciones = nc.Acciones.Where(a => a.Responsable == currentUserId).ToList();
            }
        }

        return Ok(ncs);
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

        nc.CreadoPorId = User.Identity?.Name;
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

    [Authorize] // Permitir a responsables (no solo auditores)
    [HttpPatch("acciones/{accionId}/ejecutar")]
    public async Task<IActionResult> EjecutarAccion(int accionId)
    {
        var accion = await _context.AccionesCalidad.FindAsync(accionId);
        if (accion == null) return NotFound();

        // Verificar si el usuario actual es el responsable o auditor
        var currentUserId = User.Identity?.Name;
        var isAdminOrWriter = User.IsInRole("Administrador") || User.IsInRole("Escritor");

        if (accion.Responsable != currentUserId && !isAdminOrWriter)
        {
            return Forbid("No tienes permiso para ejecutar esta acción.");
        }

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
