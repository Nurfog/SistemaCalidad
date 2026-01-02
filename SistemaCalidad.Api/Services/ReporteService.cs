using System.Text;
using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Data;

namespace SistemaCalidad.Api.Services;

public interface IReporteService
{
    Task<byte[]> GenerarListadoMaestroCsvAsync();
    Task<byte[]> GenerarReporteNoConformidadesCsvAsync();
}

public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _context;

    public ReporteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerarListadoMaestroCsvAsync()
    {
        var documentos = await _context.Documentos
            .Include(d => d.Revisiones)
            .OrderBy(d => d.Codigo)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Codigo;Titulo;Tipo;Area;Estado;Version;Fecha Actualizacion");

        foreach (var d in documentos)
        {
            csv.AppendLine($"{d.Codigo};{d.Titulo};{d.Tipo};{d.Area};{d.Estado};{d.VersionActual};{d.FechaActualizacion ?? d.FechaCreacion}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<byte[]> GenerarReporteNoConformidadesCsvAsync()
    {
        var ncs = await _context.NoConformidades
            .Include(nc => nc.Acciones)
            .OrderByDescending(nc => nc.FechaDeteccion)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Folio;Origen;Estado;Detectado Por;Fecha Deteccion;Acciones Totales;Acciones Hechas");

        foreach (var nc in ncs)
        {
            csv.AppendLine($"{nc.Folio};{nc.Origen};{nc.Estado};{nc.DetectadoPor};{nc.FechaDeteccion};{nc.Acciones.Count};{nc.Acciones.Count(a => a.FechaEjecucion != null)}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }
}
