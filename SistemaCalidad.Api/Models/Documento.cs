using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

[Table("Documentos")]
public class Documento
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public TipoDocumento Tipo { get; set; }
    
    public AreaProceso Area { get; set; }

    public EstadoDocumento Estado { get; set; }

    public int VersionActual { get; set; } = 0;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    public DateTime? FechaActualizacion { get; set; }
    
    public string? EncabezadoAdicional { get; set; }
    public string? PiePaginaPersonalizado { get; set; }

    // Propiedad de navegación para las versiones
    public virtual ICollection<VersionDocumento> Revisiones { get; set; } = new List<VersionDocumento>();

    // Relación Muchos a Muchos con Carpetas
    public virtual ICollection<CarpetaDocumento> Carpetas { get; set; } = new List<CarpetaDocumento>();
}
