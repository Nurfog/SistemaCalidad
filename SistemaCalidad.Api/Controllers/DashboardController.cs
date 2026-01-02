using Microsoft.AspNetCore.Mvc;
using SistemaCalidad.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace SistemaCalidad.Api.Controllers;

[Authorize(Roles = "Administrador,Escritor")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        return await _dashboardService.GetStatsAsync();
    }
}
