using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<QualityRecord> QualityRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Document constraints
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Code)
            .IsUnique();

        // Relationship: Document -> Versions
        modelBuilder.Entity<DocumentVersion>()
            .HasOne(v => v.Document)
            .WithMany(d => d.Revisions)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
