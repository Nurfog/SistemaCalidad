using Microsoft.EntityFrameworkCore;
using SistemaCalidad.Api.Models;

namespace SistemaCalidad.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Documento> Documentos { get; set; }
    public DbSet<VersionDocumento> VersionesDocumento { get; set; }
    public DbSet<RegistroCalidad> RegistrosCalidad { get; set; }
    public DbSet<CarpetaRegistro> CarpetasRegistros { get; set; }
    public DbSet<CarpetaDocumento> CarpetasDocumentos { get; set; }
    public DbSet<NoConformidad> NoConformidades { get; set; }
    public DbSet<AccionCalidad> AccionesCalidad { get; set; }
    public DbSet<UsuarioExterno> UsuariosExternos { get; set; }
    public DbSet<UsuarioPermiso> UsuariosPermisos { get; set; }
    public DbSet<AuditoriaAcceso> AuditoriaAccesos { get; set; }
    public DbSet<DocumentoExterno> DocumentosExternos { get; set; }
    public DbSet<Anexo> Anexos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Restricciones de Documentos
        modelBuilder.Entity<Documento>()
            .HasIndex(d => d.Codigo)
            .IsUnique();

        // Relación: Documento -> Versiones
        modelBuilder.Entity<VersionDocumento>()
            .HasOne(v => v.Documento)
            .WithMany(d => d.Revisiones)
            .HasForeignKey(v => v.DocumentoId)
            .HasForeignKey(v => v.DocumentoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación: CarpetaDocumento (Recursiva)
        modelBuilder.Entity<CarpetaDocumento>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Subcarpetas)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict); // Evitar borrado en cascada accidental de ramas enteras

        // Relación: NoConformidad -> Acciones
        modelBuilder.Entity<AccionCalidad>()
            .HasOne(a => a.NoConformidad)
            .WithMany(nc => nc.Acciones)
            .HasForeignKey(a => a.NoConformidadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NoConformidad>()
            .HasIndex(nc => nc.Folio)
            .IsUnique();

        // Mapeo Cross-Database (Schemas en MySQL)
        // Pomelo interpreta el 'schema' como el nombre de la otra base de datos
        modelBuilder.Entity<UsuarioExterno>()
            .ToTable("usuario", "sige_sam_v3", t => t.ExcludeFromMigrations());

        modelBuilder.Entity<UsuarioPermiso>()
            .ToTable("usuariospermisos");
    }
}
