using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Services;

public class StatItemDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    
    // Propiedades para compatibilidad con el frontend (que espera 'estado' o 'area')
    public string Estado => Nombre;
    public string Area => Nombre;
}

public class DashboardStatsDto
{
    public int TotalDocumentos { get; set; }
    public List<StatItemDto> DocumentosPorEstado { get; set; } = new();
    public List<StatItemDto> DocumentosPorArea { get; set; } = new();
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
    public string Nombre => Titulo; // Para compatibilidad con frontend
    public string Mensaje { get; set; } = string.Empty;
    public DateTime UltimaRevision { get; set; } // Añadido para el frontend
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
        
        stats.DocumentosPorEstado = await _context.Documentos
            .GroupBy(d => d.Estado)
            .Select(g => new StatItemDto { Nombre = g.Key.ToString(), Cantidad = g.Count() })
            .ToListAsync();
        
        stats.DocumentosPorArea = await _context.Documentos
            .GroupBy(d => d.Area)
            .Select(g => new StatItemDto { Nombre = g.Key.ToString(), Cantidad = g.Count() })
            .ToListAsync();

        // 2. Alertas de Revisión Anual
        var limiteRevision = DateTime.UtcNow.AddYears(-1);
        var vencidos = await _context.Documentos
            .Where(d => d.Estado == EstadoDocumento.Aprobado && 
                        (d.FechaActualizacion ?? d.FechaCreacion) < limiteRevision)
            .Select(d => new DocumentoAlertaDto {
                Id = d.Id,
                Codigo = d.Codigo,
                Titulo = d.Titulo,
                Mensaje = "Revisión anual pendiente.",
                UltimaRevision = d.FechaActualizacion ?? d.FechaCreacion
            })
            .ToListAsync();
        
        stats.DocumentosRevisionVencida = vencidos.Count;
        stats.AlertasCriticas = vencidos;

        // 3. Mejora Continua
        stats.NoConformidadesAbiertas = await _context.NoConformidades
            .CountAsync(nc => nc.Estado != EstadoNoConformidad.Cerrada);
        
        stats.AccionesPendientes = await _context.AccionesCalidad
            .CountAsync(a => a.FechaEjecucion == null);

        return stats;
    }
}
