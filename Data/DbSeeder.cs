using AquaPrior.Models;
using Microsoft.AspNetCore.Identity;

namespace AquaPrior.Data;

public static class DbSeeder
{
    public static void Sembrar(AquaPriorContext db)
    {
        if (db.Zonas.Any()) return;

        var nombres = new[] { "Trujillo", "El Porvenir", "Florencia de Mora", "Huanchaco", "La Esperanza",
            "Laredo", "Moche", "Poroto", "Salaverry", "Simbal", "Víctor Larco Herrera" };
        db.Zonas.AddRange(nombres.Select(n => new Zona { Nombre = n }));

        var admin = new Usuario { Nombre = "Administrador", Correo = "admin@aquaprior.pe", Rol = Rol.Administrador };
        admin.ClaveHash = new PasswordHasher<Usuario>().HashPassword(admin, "Admin123!");
        db.Usuarios.Add(admin);

        db.SaveChanges();
    }
}