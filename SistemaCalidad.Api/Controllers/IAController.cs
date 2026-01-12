using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCalidad.Api.Services;

namespace SistemaCalidad.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IAController : ControllerBase
{
    private readonly IIAService _iaService;

    public IAController(IIAService iaService)
    {
        _iaService = iaService;
    }

    [HttpPost("sincronizar")]
    public async Task<IActionResult> Sincronizar()
    {
        try
        {
            var result = await _iaService.SincronizarS3Async();
            return Ok(new { mensaje = "Sincronización de Base de Conocimientos iniciada correctamente.", detalle = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al sincronizar con la IA: {ex.Message}");
        }
    }

    [HttpPost("chat-global")]
    public async Task<IActionResult> ChatGlobal([FromBody] ChatGlobalRequest request)
    {
        try
        {
            var usuario = User.Identity?.Name ?? "anonimo";
            var respuesta = await _iaService.GenerarRespuesta(request.Pregunta, null, usuario, request.SessionId);
            return Ok(new { respuesta });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error en el chat de IA: {ex.Message}");
        }
    }

    [HttpGet("buscar-semantica")]
    public async Task<IActionResult> BuscarSemantica([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("La consulta es obligatoria.");
        
        try
        {
            var result = await _iaService.BuscarDocumentosRelacionados(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error en la búsqueda de Samito: {ex.Message}");
        }
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(_iaService.GetSamitoConfig());
    }
}

public class ChatGlobalRequest
{
    public string Pregunta { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}
