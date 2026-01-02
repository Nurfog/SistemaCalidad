using System.ComponentModel.DataAnnotations;

namespace SistemaCalidad.Api.Models;

public class QualityRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public string Identifier { get; set; } = string.Empty; // e.g. Batch number, Course ID

    public string FilePath { get; set; } = string.Empty;

    public DateTime StorageDate { get; set; } = DateTime.UtcNow;

    public int RetentionYears { get; set; } = 5; // Default for many Chilean regs

    public string StorageLocation { get; set; } = "Digital"; // Physical vs Digital

    public string ProtectionMethod { get; set; } = "Encryption & Backups";

    public bool IsDisposed { get; set; } = false;
    
    public DateTime? DisposalDate { get; set; }
}
