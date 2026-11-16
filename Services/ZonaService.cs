using AquaPrior.Models;
using AquaPrior.Repositories;

namespace AquaPrior.Services;

public class ZonaService
{
    private readonly IZonaRepository _zonas;
    private readonly DistritoLocator _locator;
    public ZonaService(IZonaRepository zonas, DistritoLocator locator)
    {
        _zonas = zonas;
        _locator = locator;
    }

    public Task<List<Zona>> ListarAsync() => _zonas.ListarConReportesAsync();
    public Task<Zona?> ObtenerAsync(int id) => _zonas.ObtenerConReportesAsync(id);

    private async Task<string?> ValidarAsync(string nombre, int? exceptoId)
    {
        if (nombre.Length < 2 || nombre.Length > 80) return "El nombre debe tener entre 2 y 80 caracteres.";
        if (!_locator.EsDistritoValido(nombre)) return "Ese distrito no está definido en el mapa territorial oficial del sistema.";
        if (await _zonas.NombreEnUsoAsync(nombre, exceptoId)) return "Ya existe un distrito con ese nombre.";
        return null;
    }

    public async Task<string?> CrearAsync(string nombre)
    {
        nombre = (nombre ?? "").Trim();
        var error = await ValidarAsync(nombre, null);
        if (error is not null) return error;
        await _zonas.AgregarAsync(new Zona { Nombre = nombre });
        return null;
    }

    public async Task<string?> ActualizarAsync(int id, string nombre)
    {
        nombre = (nombre ?? "").Trim();
        var z = await _zonas.ObtenerConReportesAsync(id);
        if (z is null) return "El distrito no existe.";
        if (z.ReporteZonas.Count > 0 && DistritoLocator.Normalizar(z.Nombre) != DistritoLocator.Normalizar(nombre))
            return "No se puede renombrar un distrito que ya tiene reportes vinculados.";
        var error = await ValidarAsync(nombre, id);
        if (error is not null) return error;
        z.Nombre = nombre;
        await _zonas.GuardarAsync();
        return null;
    }

    public async Task<string?> EliminarAsync(int id)
    {
        var z = await _zonas.ObtenerConReportesAsync(id);
        if (z is null) return "El distrito no existe.";
        if (z.ReporteZonas.Count > 0)
            return $"No se puede eliminar: el distrito tiene {z.ReporteZonas.Count} reporte(s) asociado(s).";
        await _zonas.EliminarAsync(z);
        return null;
    }
}
