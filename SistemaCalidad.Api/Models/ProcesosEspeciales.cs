using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

[Table("AuditoriaAccesos")]
public class AuditoriaAcceso
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Accion { get; set; } = string.Empty; // LOGIN, DESCARGA, CREACION, ELIMINACION, APROBACION

    [Required]
    public string Entidad { get; set; } = string.Empty; // Documento, Registro, NoConformidad

    public int EntidadId { get; set; }

    public string? Detalle { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string? IpOrigen { get; set; }
}

[Table("DocumentosExternos")]
public class DocumentoExterno
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Origen { get; set; } = string.Empty; // Ej: SENCE, Ministerio del Trabajo

    public string? VersionExterna { get; set; }

    public DateTime? FechaVigencia { get; set; }

    public string RutaArchivo { get; set; } = string.Empty;

    public string? EnlaceWeb { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public string RegistradoPor { get; set; } = string.Empty;
}
