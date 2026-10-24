using AquaPrior.Models;
using Microsoft.AspNetCore.Identity;

namespace AquaPrior.Data;

/// <summary>
/// Datos mínimos para que el sistema funcione. No crea reportes ni ciudadanos de ejemplo.
/// Cada dato se revisa por separado, así que se puede ejecutar muchas veces sin duplicar nada.
/// </summary>
public static class DbSeeder
{
    public static void Sembrar(AquaPriorContext db)
    {
        if (!db.Zonas.Any())
        {
            var nombres = new[] { "Trujillo", "El Porvenir", "Florencia de Mora", "Huanchaco", "La Esperanza",
                "Laredo", "Moche", "Poroto", "Salaverry", "Simbal", "Víctor Larco Herrera" };
            db.Zonas.AddRange(nombres.Select(n => new Zona { Nombre = n }));
        }

        const string correoAdmin = "admin@aquaprior.pe";
        if (!db.Usuarios.Any(u => u.Rol == Rol.Administrador) && !db.Usuarios.Any(u => u.Correo == correoAdmin))
        {
            var admin = new Usuario { Nombre = "Administrador", Correo = correoAdmin, Rol = Rol.Administrador };
            admin.ClaveHash = new PasswordHasher<Usuario>().HashPassword(admin, "Admin123!");
            db.Usuarios.Add(admin);
        }

        db.SaveChanges();
    }
}
