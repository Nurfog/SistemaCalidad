using Microsoft.AspNetCore.Mvc;
using SistemaCalidad.Api.Services;

namespace SistemaCalidad.Api.Controllers;

public class LoginRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("password")]
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
        try 
        {
            Console.WriteLine($"[LOGIN] Intento de acceso - Usuario: {request.Usuario}");
            
            var (success, token, rol, usuario) = await _authService.LoginAsync(request.Usuario, request.Password);

            if (!success)
            {
                Console.WriteLine($"[LOGIN] Falla: {token ?? "Credenciales incorrectas"}");
                
                if (token == "SIN_PERMISO")
                    return BadRequest(new { mensaje = "El usuario no tiene permisos para acceder a este sistema." });
                
                if (token == "CUENTA_DESACTIVADA")
                    return BadRequest(new { mensaje = "La cuenta de usuario está desactivada." });

                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            Console.WriteLine($"[LOGIN] Éxito - Usuario: {usuario}, Rol: {rol}");
            return Ok(new { token, rol, usuario });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                mensaje = "Error interno del servidor", 
                detalle = ex.Message, 
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace 
            });
        }
    }
}
