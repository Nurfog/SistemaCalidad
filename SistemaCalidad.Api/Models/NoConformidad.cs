using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

public enum EstadoNoConformidad
{
    Abierta,
    EnAnalisis,
    EnImplementacion,
    Verificada,
    Cerrada
}

public enum OrigenNoConformidad
{
    AuditoriaInterna,
    AuditoriaExterna,
    ReclamoCliente,
    RevisionDireccion,
    IncumplimientoProceso,
    SugerenciaMejora
}

[Table("NoConformidades")]
public class NoConformidad
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Folio { get; set; } = string.Empty; // Ej: NC-2026-001

    public DateTime FechaDeteccion { get; set; } = DateTime.UtcNow;

    public OrigenNoConformidad Origen { get; set; }

    [Required]
    public string DescripcionHallazgo { get; set; } = string.Empty;

    public string? AnalisisCausa { get; set; }

    public EstadoNoConformidad Estado { get; set; } = EstadoNoConformidad.Abierta;

    public string DetectadoPor { get; set; } = string.Empty;
    
    public string? ResponsableAnalisis { get; set; }

    public DateTime? FechaCierre { get; set; }

    // Relación con acciones tomadas
    public virtual ICollection<AccionCalidad> Acciones { get; set; } = new List<AccionCalidad>();
}

[Table("AccionesCalidad")]
public class AccionCalidad
{
    [Key]
    public int Id { get; set; }

    public int NoConformidadId { get; set; }
    
    [ForeignKey("NoConformidadId")]
    public virtual NoConformidad NoConformidad { get; set; } = null!;

    [Required]
    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaCompromiso { get; set; }

    public DateTime? FechaEjecucion { get; set; }

    public string Responsable { get; set; } = string.Empty;

    public bool EsEficaz { get; set; } = false;
    
    public string? ObservacionesVerificacion { get; set; }
}
