using System.Text.RegularExpressions;
using AquaPrior.Models;
using AquaPrior.Repositories;
using Microsoft.AspNetCore.Identity;

namespace AquaPrior.Services;

public class UsuarioService
{
    private static readonly Regex CorreoRx = new(@"^\S+@\S+\.\S+$");
    private readonly IUsuarioRepository _usuarios;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuarioService(IUsuarioRepository usuarios) => _usuarios = usuarios;

    public static string Normalizar(string correo) => (correo ?? "").Trim().ToLowerInvariant();

    public Task<List<Usuario>> ListarAsync() => _usuarios.ListarAsync();
    public Task<Usuario?> ObtenerAsync(int id) => _usuarios.ObtenerAsync(id);

    private async Task<string?> ValidarAsync(string nombre, string correo, int? exceptoId)
    {
        if (nombre.Length < 2 || nombre.Length > 80) return "El nombre debe tener entre 2 y 80 caracteres.";
        if (!CorreoRx.IsMatch(correo)) return "El correo no es válido.";
        var otro = await _usuarios.PorCorreoAsync(correo);
        if (otro is not null && otro.Id != exceptoId) return "Ya existe una cuenta con ese correo.";
        return null;
    }

    public async Task<(Usuario? Usuario, string? Error)> CrearAsync(string nombre, string correo, string clave, Rol rol)
    {
        nombre = (nombre ?? "").Trim();
        correo = Normalizar(correo);
        if ((clave ?? "").Length < 8) return (null, "La contraseña debe tener al menos 8 caracteres.");
        var error = await ValidarAsync(nombre, correo, null);
        if (error is not null) return (null, error);

        var u = new Usuario { Nombre = nombre, Correo = correo, Rol = rol };
        u.ClaveHash = _hasher.HashPassword(u, clave!);
        await _usuarios.AgregarAsync(u);
        return (u, null);
    }

    public async Task<string?> ActualizarAsync(int id, string nombre, string correo, Rol rol, string? nuevaClave)
    {
        nombre = (nombre ?? "").Trim();
        correo = Normalizar(correo);
        var u = await _usuarios.ObtenerAsync(id);
        if (u is null) return "El usuario no existe.";

        var error = await ValidarAsync(nombre, correo, id);
        if (error is not null) return error;
        if (!string.IsNullOrEmpty(nuevaClave) && nuevaClave.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        if (u.Rol == Rol.Administrador && rol != Rol.Administrador && await _usuarios.ContarAdministradoresAsync() <= 1)
            return "No puedes quitar el rol al único administrador.";

        u.Nombre = nombre;
        u.Correo = correo;
        u.Rol = rol;
        if (!string.IsNullOrEmpty(nuevaClave)) u.ClaveHash = _hasher.HashPassword(u, nuevaClave);
        await _usuarios.GuardarAsync();
        return null;
    }

    public async Task<string?> EliminarAsync(int id, int usuarioActualId)
    {
        var u = await _usuarios.ObtenerAsync(id);
        if (u is null) return "El usuario no existe.";
        if (u.Id == usuarioActualId) return "No puedes eliminar tu propia cuenta.";
        if (u.Reportes.Count > 0) return $"No se puede eliminar: el usuario tiene {u.Reportes.Count} reporte(s) registrado(s).";
        if (u.Rol == Rol.Administrador && await _usuarios.ContarAdministradoresAsync() <= 1)
            return "No puedes eliminar al único administrador.";
        await _usuarios.EliminarAsync(u);
        return null;
    }
}
