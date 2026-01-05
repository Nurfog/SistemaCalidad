using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

[Table("CarpetasRegistros")]
public class CarpetaRegistro
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public string Color { get; set; } = "#38bdf8"; // Color por defecto (primary)
}
