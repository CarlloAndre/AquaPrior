using AquaPrior.Models;

namespace AquaPrior.Repositories;

public interface IZonaRepository
{
    Task<List<Zona>> ListarAsync();
    Task<List<Zona>> ListarConReportesAsync();
    Task<Zona?> ObtenerAsync(int id);
    Task<Zona?> ObtenerConReportesAsync(int id);
    Task<bool> ExisteAsync(int id);
    Task<bool> NombreEnUsoAsync(string nombre, int? exceptoId);
    Task AgregarAsync(Zona zona);
    Task GuardarAsync();
    Task EliminarAsync(Zona zona);
}
