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
    public virtual Documento Documento { get; set; } = null!;

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

    public bool EsVersionActual { get; set; }
}
