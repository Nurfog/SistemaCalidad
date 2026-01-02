using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Services;

public class DashboardStatsDto
{
    public int TotalDocumentos { get; set; }
    public Dictionary<string, int> DocumentosPorEstado { get; set; } = new();
    public Dictionary<string, int> DocumentosPorArea { get; set; } = new();
    public int DocumentosRevisionVencida { get; set; } // Más de 1 año sin actualizar
    public int NoConformidadesAbiertas { get; set; }
    public int AccionesPendientes { get; set; }
    public List<DocumentoAlertaDto> AlertasCriticas { get; set; } = new();
}

public class DocumentoAlertaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var stats = new DashboardStatsDto();

        // 1. Totales y Estados
        stats.TotalDocumentos = await _context.Documentos.CountAsync();
        
        var docs = await _context.Documentos.ToListAsync();
        stats.DocumentosPorEstado = docs.GroupBy(d => d.Estado.ToString())
                                        .ToDictionary(g => g.Key, g => g.Count());
        
        stats.DocumentosPorArea = docs.GroupBy(d => d.Area.ToString())
                                      .ToDictionary(g => g.Key, g => g.Count());

        // 2. Alertas de Revisión Anual (NCh 2728 exige revisión periódica)
        var limiteRevision = DateTime.UtcNow.AddYears(-1);
        var vencidos = docs.Where(d => d.Estado == EstadoDocumento.Aprobado && 
                                     (d.FechaActualizacion ?? d.FechaCreacion) < limiteRevision).ToList();
        
        stats.DocumentosRevisionVencida = vencidos.Count;
        foreach (var v in vencidos)
        {
            stats.AlertasCriticas.Add(new DocumentoAlertaDto {
                Id = v.Id,
                Codigo = v.Codigo,
                Titulo = v.Titulo,
                Mensaje = "Revisión anual pendiente (más de 1 año sin cambios)."
            });
        }

        // 3. Mejora Continua
        stats.NoConformidadesAbiertas = await _context.NoConformidades
            .CountAsync(nc => nc.Estado != EstadoNoConformidad.Cerrada);
        
        stats.AccionesPendientes = await _context.AccionesCalidad
            .CountAsync(a => a.FechaEjecucion == null);

        return stats;
    }
}
