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
    public DbSet<NoConformidad> NoConformidades { get; set; }
    public DbSet<AccionCalidad> AccionesCalidad { get; set; }
    public DbSet<UsuarioExterno> UsuariosExternos { get; set; }
    public DbSet<UsuarioPermiso> UsuariosPermisos { get; set; }

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
            .OnDelete(DeleteBehavior.Cascade);

        // Relación: NoConformidad -> Acciones
        modelBuilder.Entity<AccionCalidad>()
            .HasOne(a => a.NoConformidad)
            .WithMany(nc => nc.Acciones)
            .HasForeignKey(a => a.NoConformidadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NoConformidad>()
            .HasIndex(nc => nc.Folio)
            .IsUnique();
    }
}
