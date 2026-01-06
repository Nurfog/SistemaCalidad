using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

// Representa la tabla en el otro schema (Base de Datos externa)
[Table("usuario")]
public class UsuarioExterno
{
    [Key]
    [Column("idUsuario")]
    public string idUsuario { get; set; } = string.Empty;

    [NotMapped]
    public string usuario => idUsuario; // El RUT ya es el usuario (string)

    [Required]
    [Column("Contrasena")]
    public string password { get; set; } = string.Empty;

    [Column("Email")]
    public string? email { get; set; } = string.Empty;

    [Column("Nombres")]
    public string? nombres { get; set; }

    [Column("ApPaterno")]
    public string? apPaterno { get; set; }

    [Required]
    [Column("Activo")]
    public int activo { get; set; } // 1 para activo, 0 para inactivo
}

// Representa los permisos en nuestro schema
[Table("usuariospermisos")]
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
