using AquaPrior.Models;

namespace AquaPrior.Repositories;

public interface IReporteRepository
{
    Task<List<Reporte>> ListarTodosAsync();
    Task<List<Reporte>> ListarPorUsuarioAsync(int usuarioId);
    Task<List<Reporte>> ListarPorEstadoAsync(EstadoReporte estado);
    Task<int> ContarPorEstadoAsync(EstadoReporte estado);
    Task<Reporte?> ObtenerAsync(int id);
    Task<Reporte?> ObtenerConDetalleAsync(int id);
    Task AgregarAsync(Reporte reporte);
    Task GuardarAsync();
    Task EliminarAsync(Reporte reporte);
}
