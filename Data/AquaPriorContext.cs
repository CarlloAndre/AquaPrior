using AquaPrior.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaPrior.Data;

public class AquaPriorContext : DbContext
{
    public AquaPriorContext(DbContextOptions<AquaPriorContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Reporte> Reportes => Set<Reporte>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();
        b.Entity<Zona>().HasIndex(z => z.Nombre).IsUnique();
        b.Entity<Reporte>().HasOne(r => r.Usuario).WithMany(u => u.Reportes)
            .HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Reporte>().HasOne(r => r.Zona).WithMany(z => z.Reportes)
            .HasForeignKey(r => r.ZonaId).OnDelete(DeleteBehavior.Restrict);
    }
}
