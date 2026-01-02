using Microsoft.AspNetCore.Mvc;
using SistemaCalidad.Api.Services;

namespace SistemaCalidad.Api.Controllers;

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Usuario, request.Password);

        if (!result.Success)
        {
            if (result.Token == "SIN_PERMISO")
                return Forbidden("No tienes permisos asignados para acceder a la plataforma de Calidad.");
            
            return Unauthorized("Usuario o contraseña incorrectos.");
        }

        return Ok(new
        {
            usuario = result.Usuario,
            rol = result.Rol,
            mensaje = $"Bienvenido al Sistema de Calidad (Modo {result.Rol})",
            token = result.Token
        });
    }

    private IActionResult Forbidden(string message)
    {
        return StatusCode(403, new { mensaje = message });
    }
}
