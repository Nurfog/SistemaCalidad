using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

[Table("Anexos")]
public class Anexo
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public string RutaArchivo { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Formato { get; set; } = string.Empty; // PDF, DOCX, XLSX

    public bool EsObligatorio { get; set; } = false;

    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    public DateTime? UltimaActualizacion { get; set; }
}
