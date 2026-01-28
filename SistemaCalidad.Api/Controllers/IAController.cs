using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaCalidad.Api.Services;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;

namespace SistemaCalidad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IAController : ControllerBase
{
    private readonly ILocalRAGService _ragService;
    private readonly ApplicationDbContext _context;

    public IAController(ILocalRAGService ragService, ApplicationDbContext context)
    {
        _ragService = ragService;
        _context = context;
    }

    [Authorize]
    [HttpGet("buscar-semantica")]
    public async Task<IActionResult> BuscarSemantica([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { mensaje = "La consulta no puede estar vacía." });
            }

            // Buscar segmentos similares usando RAG local
            var segmentos = await _ragService.BuscarSimilares(query, 5);

            if (segmentos == null || !segmentos.Any())
            {
                return Ok(new 
                { 
                    resena = "No encontré documentos relevantes para tu búsqueda.",
                    codigosArchivos = new List<string>()
                });
            }

            // Obtener los IDs únicos de documentos
            var documentoIds = segmentos.Select(s => s.DocumentoId).Distinct().ToList();

            // Obtener los códigos de los documentos
            var documentos = await _context.Documentos
                .Where(d => documentoIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Codigo, d.Titulo })
                .ToListAsync();

            // Construir resumen
            var resumen = $"Encontré {documentos.Count} documento(s) relevante(s): " +
                         string.Join(", ", documentos.Select(d => $"{d.Codigo} - {d.Titulo}"));

            return Ok(new
            {
                resena = resumen,
                codigosArchivos = documentos.Select(d => d.Codigo).ToList()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IAController] Error en búsqueda semántica: {ex}");
            return StatusCode(500, new { mensaje = $"Error al procesar búsqueda: {ex.Message}" });
        }
    }
}
