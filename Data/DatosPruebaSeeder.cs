using AquaPrior.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AquaPrior.Data;

/// <summary>Genera un conjunto repetible de datos sintéticos para desarrollo local.</summary>
public static class DatosPruebaSeeder
{
    private const string Marcador = "AquaPrior datos de demostración";

    public static async Task SembrarAsync(AquaPriorContext db)
    {
        var zonas = await db.Zonas.ToDictionaryAsync(z => z.Nombre.ToLowerInvariant());
        var nombresZonas = new[]
        {
            "Trujillo", "Víctor Larco Herrera", "La Esperanza", "El Porvenir", "Florencia de Mora",
            "Huanchaco", "Laredo", "Moche", "Salaverry", "Alto Trujillo"
        };
        if (nombresZonas.Any(nombre => !zonas.ContainsKey(nombre.ToLowerInvariant()))) return;

        var reportesPruebaExistentes = (await db.Reportes
            .Where(r => r.Motivo == Marcador)
            .Select(r => r.Direccion)
            .ToListAsync())
            .ToHashSet();

        var hasher = new PasswordHasher<Usuario>();
        var usuarios = new List<Usuario>();
        for (var i = 1; i <= 8; i++)
        {
            var correo = $"prueba{i}@aquaprior.local";
            var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo);
            if (usuario is null)
            {
                usuario = new Usuario { Nombre = $"Vecino de prueba {i}", Correo = correo };
                usuario.ClaveHash = hasher.HashPassword(usuario, "Prueba123!");
                db.Usuarios.Add(usuario);
            }
            usuarios.Add(usuario);
        }

        await db.SaveChangesAsync();

        var ahora = DateTime.Now;
        var reportes = new List<Reporte>();
        AgregarIncidente("Trujillo", TipoProblema.CorteTotal, -8.1110, -79.0280, 18, 210, 4);
        AgregarIncidente("Trujillo", TipoProblema.CorteTotal, -8.1030, -79.0350, 10, 130, 2);
        AgregarIncidente("Víctor Larco Herrera", TipoProblema.CorteTotal, -8.1350, -79.0500, 7, 540, 5);
        AgregarIncidente("Víctor Larco Herrera", TipoProblema.CorteTotal, -8.1450, -79.0450, 4, 180, 3);
        AgregarIncidente("La Esperanza", TipoProblema.CorteTotal, -8.0500, -79.0400, 3, 90, 3);
        AgregarIncidente("El Porvenir", TipoProblema.CorteTotal, -8.0650, -78.9900, 9, 280, 4);
        AgregarIncidente("Florencia de Mora", TipoProblema.CorteTotal, -8.0750, -79.0200, 5, 170, 3);
        AgregarIncidente("Huanchaco", TipoProblema.CorteTotal, -8.0800, -79.1150, 12, 320, 4);
        AgregarIncidente("Laredo", TipoProblema.CorteTotal, -8.0890, -78.9600, 6, 140, 3);
        AgregarIncidente("Moche", TipoProblema.CorteTotal, -8.1710, -79.0090, 2, 110, 2);
        AgregarIncidente("Salaverry", TipoProblema.CorteTotal, -8.2260, -78.9770, 8, 230, 3);
        AgregarIncidente("Alto Trujillo", TipoProblema.CorteTotal, -8.0350, -78.9900, 3, 200, 3);
        AgregarIncidente("Víctor Larco Herrera", TipoProblema.BajaPresion, -8.1280, -79.0450, 2, 75, 2);

        // Añade demostraciones nuevas hasta completar 50 reportes en Víctor Larco.
        var reportesLarcoExistentes = await db.Reportes.CountAsync(r =>
            r.Motivo == Marcador && r.Zonas.Any(rz => rz.Zona.Nombre == "Víctor Larco Herrera"));
        var faltantes = Math.Max(0, 50 - reportesLarcoExistentes);
        AgregarIncidente("Víctor Larco Herrera", TipoProblema.CorteTotal, -8.1370, -79.0480, 2, 60,
            Math.Min(faltantes, 12), 0.00004);
        faltantes -= Math.Min(faltantes, 12);
        AgregarIncidente("Víctor Larco Herrera", TipoProblema.CorteTotal, -8.1395, -79.0500, 4, 120,
            Math.Min(faltantes, 16), 0.00010);
        faltantes -= Math.Min(faltantes, 16);
        AgregarIncidente("Víctor Larco Herrera", TipoProblema.CorteTotal, -8.1420, -79.0470, 6, 220,
            faltantes, 0.00010);

        // Completa hasta 560 reportes ficticios en total, distribuidos por toda la provincia.
        var objetivoTotal = Math.Max(560, reportesPruebaExistentes.Count);
        var centros = new (string Zona, double Lat, double Lng)[]
        {
            ("Trujillo", -8.1110, -79.0280),
            ("Víctor Larco Herrera", -8.1370, -79.0480),
            ("La Esperanza", -8.0500, -79.0400),
            ("El Porvenir", -8.0650, -78.9900),
            ("Florencia de Mora", -8.0750, -79.0200),
            ("Huanchaco", -8.0800, -79.1150),
            ("Laredo", -8.0890, -78.9600),
            ("Moche", -8.1710, -79.0090),
            ("Salaverry", -8.2260, -78.9770),
            ("Alto Trujillo", -8.0350, -78.9900)
        };
        var numero = 0;
        while (reportesPruebaExistentes.Count + reportes.Count < objetivoTotal)
        {
            var centro = centros[numero % centros.Length];
            var grupo = numero / centros.Length;
            var cantidad = Math.Min(8, objetivoTotal - reportesPruebaExistentes.Count - reportes.Count);
            AgregarIncidente(centro.Zona, TipoProblema.CorteTotal,
                centro.Lat + grupo * 0.0012, centro.Lng + grupo * 0.0012,
                1 + numero % 18, 80 + numero % 420, cantidad, 0.000035);
            numero++;
        }

        var nuevos = reportes.Where(r => !reportesPruebaExistentes.Contains(r.Direccion)).ToList();
        db.Reportes.AddRange(nuevos);
        await db.SaveChangesAsync();
        var totalLarco = await db.Reportes.CountAsync(r =>
            r.Motivo == Marcador && r.Zonas.Any(rz => rz.Zona.Nombre == "Víctor Larco Herrera"));
        var totalPrueba = await db.Reportes.CountAsync(r => r.Motivo == Marcador);
        Console.WriteLine($"AquaPrior: se agregaron {nuevos.Count} reportes ficticios. Total: {totalPrueba}/560. Víctor Larco: {totalLarco}.");

        void AgregarIncidente(string zonaNombre, TipoProblema tipo, double lat, double lng, double horas, int personas, int cantidad, double separacion = 0.00005)
        {
            for (var i = 0; i < cantidad; i++)
            {
                var usuario = usuarios[reportes.Count % usuarios.Count];
                reportes.Add(new Reporte
                {
                    Usuario = usuario,
                    Direccion = $"Punto de prueba {reportes.Count + 1}",
                    Motivo = Marcador,
                    Latitud = lat + i * separacion,
                    Longitud = lng + i * separacion,
                    InicioCorte = ahora.AddHours(-horas),
                    FechaRegistro = ahora.AddMinutes(-i * 4),
                    Tipo = tipo,
                    Estado = EstadoReporte.Verificado,
                    Atencion = EstadoAtencion.EnAtencion,
                    Zonas = new List<ReporteZona>
                    {
                        new() { Zona = zonas[zonaNombre.ToLowerInvariant()], PersonasAfectadas = personas }
                    }
                });
            }
        }
    }
}
