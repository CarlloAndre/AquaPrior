using AquaPrior.Data;
using AquaPrior.Models;
using Microsoft.EntityFrameworkCore;

namespace AquaPrior.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AquaPriorContext _db;
    public UsuarioRepository(AquaPriorContext db) => _db = db;

    public Task<List<Usuario>> ListarAsync() =>
        _db.Usuarios.Include(u => u.Reportes).OrderBy(u => u.Nombre).ToListAsync();

    public Task<Usuario?> ObtenerAsync(int id) =>
        _db.Usuarios.Include(u => u.Reportes).FirstOrDefaultAsync(u => u.Id == id);

    public Task<Usuario?> PorCorreoAsync(string correo) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);

    public Task<int> ContarAdministradoresAsync() =>
        _db.Usuarios.CountAsync(u => u.Rol == Rol.Administrador);

    public async Task AgregarAsync(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
    }

    public Task GuardarAsync() => _db.SaveChangesAsync();

    public async Task EliminarAsync(Usuario usuario)
    {
        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
    }
}
