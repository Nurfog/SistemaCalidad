using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;

namespace SistemaCalidad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthCheckController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Check()
    {
        var status = new {
            Database = "Unknown",
            ServerTime = DateTime.UtcNow,
            Message = "API is reachable"
        };

        try
        {
            // Intentar una consulta simple
            var canConnect = await _context.Database.CanConnectAsync();
            var foldersCount = await _context.CarpetasDocumentos.CountAsync();
            var docsCount = await _context.Documentos.CountAsync();
            
            // Listar bases de datos disponibles
            var databases = new List<string>();
            try {
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SHOW DATABASES";
                    _context.Database.OpenConnection();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) databases.Add(reader.GetString(0));
                    }
                }
            } catch (Exception ex) {
                databases.Add("Error listing: " + ex.Message);
            }

            return Ok(new 
            { 
                Database = canConnect ? "Connected" : "Disconnected",
                AvailableDatabases = databases,
                Folders = foldersCount,
                Documents = docsCount,
                ServerTime = DateTime.UtcNow,
                Message = "API Health Check - Schema Discovery"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                Database = "Error",
                Error = ex.Message,
                ServerTime = DateTime.UtcNow
            });
        }
    }
}
