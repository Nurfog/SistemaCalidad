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
        // 1. Buscar el usuario en el schema externo sige_sam_v3
        var usuarioExterno = await _context.UsuariosExternos
            .FirstOrDefaultAsync(u => u.usuario == username);

        if (usuarioExterno == null) return (false, "", "", "");

        // 2. Verificar estado activo (1=Activo, 0=Inactivo)
        if (usuarioExterno.activo != 1) return (false, "CUENTA_DESACTIVADA", "", "");

        // 3. Verificar password (Texto plano o SHA-1)
        bool isPasswordValid = false;
        
        if (usuarioExterno.password.Length > 20)
        {
            // Es un hash SHA-1
            isPasswordValid = VerifySha1(password, usuarioExterno.password);
        }
        else
        {
            // Es texto plano
            isPasswordValid = (password == usuarioExterno.password);
        }

        if (!isPasswordValid) return (false, "", "", "");

        // 3. Verificar si tiene permiso en el sistema de Calidad
        var permiso = await _context.UsuariosPermisos
            .FirstOrDefaultAsync(p => p.UsuarioIdExterno == usuarioExterno.id && p.Activo);

        if (permiso == null) return (false, "SIN_PERMISO", "", "");

        // 4. Generar Token JWT Real
        var token = GenerateJwtToken(usuarioExterno.usuario, permiso.Rol);

        return (true, token, permiso.Rol, usuarioExterno.usuario);
    }

    private string GenerateJwtToken(string username, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
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

    private bool VerifySha1(string input, string hash)
    {
        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        
        // Comparamos el hash generado con el guardado (case insensitive)
        return sb.ToString().Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}
