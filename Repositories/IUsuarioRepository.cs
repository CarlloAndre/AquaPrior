using AquaPrior.Models;

namespace AquaPrior.Repositories;

public interface IUsuarioRepository
{
    Task<List<Usuario>> ListarAsync();
    Task<Usuario?> ObtenerAsync(int id);
    Task<Usuario?> PorCorreoAsync(string correo);
    Task<int> ContarAdministradoresAsync();
    Task AgregarAsync(Usuario usuario);
    Task GuardarAsync();
    Task EliminarAsync(Usuario usuario);
}
