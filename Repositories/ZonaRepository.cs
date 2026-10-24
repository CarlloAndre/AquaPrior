using AquaPrior.Data;
using AquaPrior.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaPrior.Repositories;

public class ZonaRepository : IZonaRepository
{
    private readonly AquaPriorContext _db;
    public ZonaRepository(AquaPriorContext db) => _db = db;

    public Task<List<Zona>> ListarAsync() => _db.Zonas.OrderBy(z => z.Nombre).ToListAsync();

    public Task<List<Zona>> ListarConReportesAsync() =>
        _db.Zonas.Include(z => z.ReporteZonas).ThenInclude(rz => rz.Reporte).OrderBy(z => z.Nombre).ToListAsync();

    public Task<Zona?> ObtenerAsync(int id) => _db.Zonas.FirstOrDefaultAsync(z => z.Id == id);

    public Task<Zona?> ObtenerConReportesAsync(int id) =>
        _db.Zonas
            .Include(z => z.ReporteZonas).ThenInclude(rz => rz.Reporte).ThenInclude(r => r.Usuario)
            .Include(z => z.ReporteZonas).ThenInclude(rz => rz.Reporte).ThenInclude(r => r.Zonas)
            .FirstOrDefaultAsync(z => z.Id == id);

    public Task<bool> ExisteAsync(int id) => _db.Zonas.AnyAsync(z => z.Id == id);

    public Task<bool> NombreEnUsoAsync(string nombre, int? exceptoId) =>
        _db.Zonas.AnyAsync(z => z.Nombre == nombre && (exceptoId == null || z.Id != exceptoId));

    public async Task AgregarAsync(Zona zona)
    {
        _db.Zonas.Add(zona);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public async Task EliminarAsync(Zona zona)
    {
        _db.Zonas.Remove(zona);
        await _db.SaveChangesAsync();
    }
}