using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaCalidad.Api.Models;

public class DocumentVersion
{
    [Key]
    public int Id { get; set; }

    public int DocumentId { get; set; }
    
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    public int VersionNumber { get; set; }

    [Required]
    public string ChangeDescription { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;
    
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public string CreatedBy { get; set; } = string.Empty;

    public string? ReviewedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public bool IsCurrent { get; set; }
}
