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

        // 1. Intentar buscar primero en SIGE (Usuarios Externos)
        var usuarioExterno = await _context.UsuariosExternos
            .FromSqlRaw("SELECT idUsuario, Contrasena, Activo, Email, Nombres, ApPaterno FROM sige_sam_v3.usuario WHERE idUsuario = {0}", username)
            .FirstOrDefaultAsync();

        if (usuarioExterno != null)
        {
            // Validar Password SIGE
            bool isPasswordValid = false;
            if (usuarioExterno.password.Length > 20)
                isPasswordValid = VerifySha256(password, usuarioExterno.password);
            else
                isPasswordValid = (password == usuarioExterno.password);

            if (isPasswordValid && usuarioExterno.activo == 1)
            {
                // Verificar si tiene permiso en nuestro sistema
                if (int.TryParse(usuarioExterno.idUsuario, out int idInt))
                {
                    var permiso = await _context.UsuariosPermisos
                        .FirstOrDefaultAsync(p => p.UsuarioIdExterno == idInt && p.Activo);

                    if (permiso != null)
                    {
                        var fullName = $"{usuarioExterno.nombres} {usuarioExterno.apPaterno}".Trim();
                        if (string.IsNullOrEmpty(fullName)) fullName = usuarioExterno.usuario;

                        var token = GenerateJwtToken(usuarioExterno.usuario, permiso.Rol, fullName);
                        return (true, token, permiso.Rol, fullName);
                    }
                }
            }
        }

        // 2. Si no es usuario SIGE o falló, buscar en Usuarios Locales (Auditores Externos, etc.)
        if (int.TryParse(username, out int rutInt))
        {
            var permisoLocal = await _context.UsuariosPermisos
                .FirstOrDefaultAsync(p => p.UsuarioIdExterno == rutInt && p.Activo && !string.IsNullOrEmpty(p.PasswordHash));

            if (permisoLocal != null)
            {
                if (VerifySha256(password, permisoLocal.PasswordHash!))
                {
                    var token = GenerateJwtToken(username, permisoLocal.Rol, permisoLocal.NombreCompleto ?? username);
                    return (true, token, permisoLocal.Rol, permisoLocal.NombreCompleto ?? username);
                }
            }
        }

        return (false, "", "", "");
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
