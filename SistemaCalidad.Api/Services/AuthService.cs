using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Services;

public interface IAuthService
{
    Task<(bool Success, string Token, string Rol, string Usuario)> LoginAsync(string username, string password);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Token, string Rol, string Usuario)> LoginAsync(string username, string password)
    {
        username = username.Trim();
        password = password.Trim();
        // 1. Buscar el usuario en el schema externo sige_sam_v3 utilizando idUsuario (RUT)
        // Seleccionamos campos específicos para evitar conflictos con columnas que no conocemos
        var usuarioExterno = await _context.UsuariosExternos
            .FromSqlRaw("SELECT idUsuario, Contrasena, Activo, Email, Nombres, ApPaterno FROM sige_sam_v3.usuario WHERE idUsuario = {0}", username)
            .FirstOrDefaultAsync();

        if (usuarioExterno == null) 
        {
            Console.WriteLine($"[AUTH] Usuario {username} no encontrado en sige_sam_v3.usuario");
            return (false, "", "", "");
        }

        // 2. Verificar estado activo (1=Activo, 0=Inactivo)
        if (usuarioExterno.activo != 1) 
        {
            Console.WriteLine($"[AUTH] Usuario {username} desactivado (Activo={usuarioExterno.activo})");
            return (false, "CUENTA_DESACTIVADA", "", "");
        }

        // 3. Verificar password (Texto plano o SHA-1)
        bool isPasswordValid = false;
        
        if (usuarioExterno.password.Length > 20)
        {
            // Es un hash SHA-256
            isPasswordValid = VerifySha256(password, usuarioExterno.password);
            Console.WriteLine($"[AUTH] Verificando SHA256 para {username}: {isPasswordValid}");
        }
        else
        {
            // Es texto plano
            isPasswordValid = (password == usuarioExterno.password);
            Console.WriteLine($"[AUTH] Verificando Texto Plano para {username}: {isPasswordValid}");
        }

        if (!isPasswordValid) return (false, "", "", "");

        // 3. Verificar si tiene permiso en el sistema de Calidad
        // Convertimos el idUsuario (string en SIGE) a int para comparar con nuestra tabla (INT)
        if (!int.TryParse(usuarioExterno.idUsuario, out int idInt))
        {
            Console.WriteLine($"[AUTH] Error al convertir RUT {usuarioExterno.idUsuario} a entero para búsqueda de permisos.");
            return (false, "ERROR_SISTEMA", "", "");
        }

        var permiso = await _context.UsuariosPermisos
            .FirstOrDefaultAsync(p => p.UsuarioIdExterno == idInt && p.Activo);

        if (permiso == null) 
        {
            Console.WriteLine($"[AUTH] Usuario {username} (ID: {idInt}) no tiene permisos en sistemacalidad_nch2728");
            return (false, "SIN_PERMISO", "", "");
        }

        // 4. Generar Token JWT Real
        var fullName = $"{usuarioExterno.nombres} {usuarioExterno.apPaterno}".Trim();
        if (string.IsNullOrEmpty(fullName)) fullName = usuarioExterno.usuario;

        var token = GenerateJwtToken(usuarioExterno.usuario, permiso.Rol, fullName);

        return (true, token, permiso.Rol, fullName);
    }

    private string GenerateJwtToken(string username, string role, string fullName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("FullName", fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(8),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }

    private bool VerifySha256(string input, string hash)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        
        var inputHash = sb.ToString();
        Console.WriteLine($"[AUTH] Generado: {inputHash}");
        Console.WriteLine($"[AUTH] En Base:  {hash}");

        // Comparamos el hash generado con el guardado (case insensitive)
        return inputHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}
