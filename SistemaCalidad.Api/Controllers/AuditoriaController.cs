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
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _context.AuditoriaAccesos
            .OrderByDescending(a => a.Fecha)
            .Take(200)
            .ToListAsync();

        // Obtener IDs de usuarios únicos de los logs
        var userIds = logs.Select(l => l.Usuario).Distinct().ToList();

        // Buscar nombres en SIGE (External)
        var usuariosExternos = await _context.UsuariosExternos
            .Where(u => userIds.Contains(u.idUsuario))
            .ToDictionaryAsync(u => u.idUsuario, u => $"{u.nombres} {u.apPaterno}".Trim());

        // Buscar nombres en Usuarios Locales (Permisos)
        var usuariosLocales = await _context.UsuariosPermisos
            .Where(u => userIds.Contains(u.UsuarioIdExterno.ToString()))
            .ToDictionaryAsync(u => u.UsuarioIdExterno.ToString(), u => u.NombreCompleto ?? u.UsuarioIdExterno.ToString());

        // Mapear logs con nombres
        var result = logs.Select(l => new {
            l.Id,
            l.Accion,
            l.Entidad,
            l.EntidadId,
            l.Detalle,
            l.Fecha,
            l.IpOrigen,
            Usuario = usuariosExternos.ContainsKey(l.Usuario) ? usuariosExternos[l.Usuario] : 
                      usuariosLocales.ContainsKey(l.Usuario) ? usuariosLocales[l.Usuario] : 
                      l.Usuario
        });

        return Ok(result);
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
