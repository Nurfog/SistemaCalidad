using Microsoft.AspNetCore.Mvc;
using SistemaCalidad.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[Authorize(Roles = "Administrador,Escritor")]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("listado-maestro")]
    public async Task<IActionResult> GetListadoMaestro()
    {
        var csv = await _reporteService.GenerarListadoMaestroCsvAsync();
        return File(csv, "text/csv", $"Listado_Maestro_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("no-conformidades")]
    public async Task<IActionResult> GetReporteNC()
    {
        var csv = await _reporteService.GenerarReporteNoConformidadesCsvAsync();
        return File(csv, "text/csv", $"Reporte_NC_{DateTime.Now:yyyyMMdd}.csv");
    }
}
