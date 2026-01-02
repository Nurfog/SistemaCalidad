using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

// Representa la tabla en el otro schema
[Table("usuarios", Schema = "sige_sam_v3")]
public class UsuarioExterno
{
    [Key]
    public int id { get; set; }

    [Required]
    public string usuario { get; set; } = string.Empty;

    [Required]
    public string password { get; set; } = string.Empty;

    public string? email { get; set; } = string.Empty;

    [Required]
    public int activo { get; set; } // 1 para activo, 0 para inactivo
}

// Representa los permisos en nuestro schema
[Table("UsuariosPermisos", Schema = "sistemacalidad_nch2728")]
public class UsuarioPermiso
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int UsuarioIdExterno { get; set; }

    [Required]
    public string Rol { get; set; } = "Lector"; // Administrador, Escritor, Lector

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;
}
