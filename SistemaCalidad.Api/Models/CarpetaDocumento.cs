using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaCalidad.Api.Models;

[Table("CarpetasDocumentos")]
public class CarpetaDocumento
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public string Color { get; set; } = "#fbbf24"; // Amber default for Documents

    // Recursividad: Carpeta Padre
    public int? ParentId { get; set; }
    
    [ForeignKey("ParentId")]
    [JsonIgnore] // Evitar ciclos infinitos en serialización
    public CarpetaDocumento? Parent { get; set; }

    [JsonPropertyName("subCarpetas")] // Frontend espera camelCase con 'C'
    public ICollection<CarpetaDocumento> Subcarpetas { get; set; } = new List<CarpetaDocumento>();

    // Relación con Documentos (One-to-Many)
    // Se definirá en Documento.cs o via DbContext, pero es útil tener la colección aquí si se usa EF Core
    [JsonIgnore]
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
}
