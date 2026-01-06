using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaCalidad.Api.Models;

[Table("VersionesDocumento")]
public class VersionDocumento
{
    [Key]
    public int Id { get; set; }

    public int DocumentoId { get; set; }
    
    [ForeignKey("DocumentoId")]
    [JsonIgnore]
    public virtual Documento? Documento { get; set; }

    public int NumeroVersion { get; set; }

    [Required]
    public string DescripcionCambio { get; set; } = string.Empty;

    public string RutaArchivo { get; set; } = string.Empty;
    
    public string NombreArchivo { get; set; } = string.Empty;

    public string TipoContenido { get; set; } = string.Empty;

    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;

    public string CreadoPor { get; set; } = string.Empty;

    public string? RevisadoPor { get; set; }

    public string? AprobadoPor { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    // Flujo de Aprobación
    public string EstadoRevision { get; set; } = "Aprobado"; // Pendiente, Aprobado, Rechazado
    public string? ObservacionesRevision { get; set; }
    public string? RevisadoPorId { get; set; } // RUT del Auditor Interno
    public DateTime? FechaRevision { get; set; }
    public bool EsVersionActual { get; set; }
}
