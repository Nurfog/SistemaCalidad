using System.ComponentModel.DataAnnotations;

namespace SistemaCalidad.Api.Models;

public class Document
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DocumentType Type { get; set; }
    
    public ProcessArea Area { get; set; }

    public DocumentStatus Status { get; set; }

    public int CurrentVersion { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    // Navigation property for versions
    public virtual ICollection<DocumentVersion> Revisions { get; set; } = new List<DocumentVersion>();
}
