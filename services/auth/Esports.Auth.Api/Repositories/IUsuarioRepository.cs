using Esports.Auth.Api.Domain;

namespace Esports.Auth.Api.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByUsernameAsync(string username);
    // Devuelve true si fue insertado; false si ya existía (LWT IF NOT EXISTS).
    Task<bool> CreateAsync(Usuario usuario);
}
