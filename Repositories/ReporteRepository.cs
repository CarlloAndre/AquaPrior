using AquaPrior.Data;
using AquaPrior.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaPrior.Repositories;

public class ReporteRepository : IReporteRepository
{
    private readonly AquaPriorContext _db;
    public ReporteRepository(AquaPriorContext db) => _db = db;

    // Consulta base: el reporte con sus líneas de detalle y el nombre de cada distrito.
    private IQueryable<Reporte> ConZonas() =>
        _db.Reportes.Include(r => r.Zonas).ThenInclude(rz => rz.Zona);

    public Task<List<Reporte>> ListarTodosAsync() =>
        ConZonas().Include(r => r.Usuario).OrderByDescending(r => r.Id).ToListAsync();

    public Task<List<Reporte>> ListarPorUsuarioAsync(int usuarioId) =>
        ConZonas().Where(r => r.UsuarioId == usuarioId).OrderByDescending(r => r.Id).ToListAsync();

    public Task<List<Reporte>> ListarPorEstadoAsync(EstadoReporte estado) =>
        ConZonas().Where(r => r.Estado == estado).ToListAsync();

    public Task<int> ContarPorEstadoAsync(EstadoReporte estado) =>
        _db.Reportes.CountAsync(r => r.Estado == estado);

    public Task<Reporte?> ObtenerAsync(int id) =>
        _db.Reportes.FirstOrDefaultAsync(r => r.Id == id);

    public Task<Reporte?> ObtenerConDetalleAsync(int id) =>
        ConZonas().Include(r => r.Usuario).FirstOrDefaultAsync(r => r.Id == id);

    public async Task AgregarAsync(Reporte reporte)
    {
        _db.Reportes.Add(reporte);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public async Task EliminarAsync(Reporte reporte)
    {
        _db.Reportes.Remove(reporte);   // las líneas de detalle se eliminan en cascada
        await _db.SaveChangesAsync();
    }

    public void QuitarLineas(IEnumerable<ReporteZona> lineas) => _db.ReporteZonas.RemoveRange(lineas);
}
