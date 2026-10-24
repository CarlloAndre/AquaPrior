using AquaPrior.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaPrior.Data;

public class AquaPriorContext : DbContext
{
    public AquaPriorContext(DbContextOptions<AquaPriorContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<ReporteZona> ReporteZonas => Set<ReporteZona>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();
        b.Entity<Zona>().HasIndex(z => z.Nombre).IsUnique();

        b.Entity<Reporte>().HasOne(r => r.Usuario).WithMany(u => u.Reportes)
            .HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        // Tabla intermedia con clave compuesta: un distrito aparece una sola vez por reporte.
        b.Entity<ReporteZona>().HasKey(rz => new { rz.ReporteId, rz.ZonaId });
        b.Entity<ReporteZona>().HasOne(rz => rz.Reporte).WithMany(r => r.Zonas)
            .HasForeignKey(rz => rz.ReporteId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReporteZona>().HasOne(rz => rz.Zona).WithMany(z => z.ReporteZonas)
            .HasForeignKey(rz => rz.ZonaId).OnDelete(DeleteBehavior.Restrict);
    }
}
