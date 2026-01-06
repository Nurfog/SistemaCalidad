using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsuariosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public class AsignarPermisoDto
    {
        public int? UsuarioId { get; set; } // Opcional si es externo local
        public string Rut { get; set; } = string.Empty;
        public string Rol { get; set; } = "Lector";
        public string? NombreCompleto { get; set; } // Para externos
        public string? Password { get; set; } // Para externos
        public string? Email { get; set; } // Para externos
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> GetUsuariosConPermiso()
    {
        var permisos = await _context.UsuariosPermisos.ToListAsync();
        
        // Unir con nombres de SIGE si es posible
        var rutsSige = permisos.Where(p => string.IsNullOrEmpty(p.NombreCompleto)).Select(p => p.UsuarioIdExterno.ToString()).ToList();
        
        var nombresSige = new Dictionary<string, string>();
        if (rutsSige.Any())
        {
            var rutsQuery = string.Join(",", rutsSige.Select(r => $"'{r}'"));
            var usuariosSige = await _context.UsuariosExternos
                .FromSqlRaw($"SELECT idUsuario, Contrasena, Activo, Email, Nombres, ApPaterno FROM sige_sam_v3.usuario WHERE idUsuario IN ({rutsQuery})")
                .ToListAsync();
            
            foreach(var u in usuariosSige) nombresSige[u.idUsuario] = $"{u.nombres} {u.apPaterno}".Trim();
        }

        var result = permisos.Select(p => new {
            Rut = p.UsuarioIdExterno,
            p.Rol,
            p.Activo,
            Nombre = p.NombreCompleto ?? (nombresSige.ContainsKey(p.UsuarioIdExterno.ToString()) ? nombresSige[p.UsuarioIdExterno.ToString()] : "Usuario SIGE"),
            p.EmailExterno,
            EsLocal = !string.IsNullOrEmpty(p.PasswordHash)
        });

        return Ok(result);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("asignar")]
    public async Task<IActionResult> AsignarPermiso(AsignarPermisoDto dto)
    {
        var permiso = await _context.UsuariosPermisos.FirstOrDefaultAsync(p => p.UsuarioIdExterno.ToString() == dto.Rut);
        
        if (permiso == null)
        {
            permiso = new UsuarioPermiso
            {
                UsuarioIdExterno = int.Parse(dto.Rut),
                Rol = dto.Rol,
                Activo = true,
                FechaAsignacion = DateTime.UtcNow
            };
            _context.UsuariosPermisos.Add(permiso);
        }
        else
        {
            permiso.Rol = dto.Rol;
        }

        // Si es Auditor Externo y se provee password, es un usuario local
        if (dto.Rol == "AuditorExterno" && !string.IsNullOrEmpty(dto.Password))
        {
            permiso.NombreCompleto = dto.NombreCompleto;
            permiso.EmailExterno = dto.Email;
            // Usamos el hash de AuthService para consistencia si estuviera expuesto, 
            // sino implementamos SHA256 básico aquí (en este entorno es accesible)
            permiso.PasswordHash = HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Permiso asignado correctamente" });
    }

    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        var sb = new System.Text.StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetUsuariosActivos()
    {
        // Recuperamos los usuarios activos del sistema central
        var usuarios = await _context.UsuariosExternos
            .FromSqlRaw("SELECT idUsuario, Contrasena, Activo, Email, Nombres, ApPaterno FROM sige_sam_v3.usuario WHERE Activo = 1")
            .Select(u => new {
                Id = u.usuario, // El RUT/ID usuario
                NombreCompleto = $"{u.nombres} {u.apPaterno}".Trim()
            })
            .ToListAsync();

        return Ok(usuarios);
    }
}
