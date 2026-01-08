namespace SistemaCalidad.Api.Models;

public class DocumentoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; }
    public string Titulo { get; set; }
    public TipoDocumento Tipo { get; set; }
    public AreaProceso Area { get; set; }
    public EstadoDocumento Estado { get; set; }
    public int VersionActual { get; set; }
    public string? NombreArchivoActual { get; set; } // Para descarga directa sin buscar en lista
    public DateTime FechaActualizacion { get; set; }
}
