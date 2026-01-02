using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

[Table("RegistrosCalidad")]
public class RegistroCalidad
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public string Identificador { get; set; } = string.Empty; // Ej: ID de curso, folio

    public string RutaArchivo { get; set; } = string.Empty;

    public DateTime FechaAlmacenamiento { get; set; } = DateTime.UtcNow;

    public int AnosRetencion { get; set; } = 5; 

    public string UbicacionAlmacenamiento { get; set; } = "Digital";

    public string MetodoProteccion { get; set; } = "Cifrado y Backups";

    public bool EstaEliminado { get; set; } = false;
    
    public DateTime? FechaEliminacion { get; set; }
}
