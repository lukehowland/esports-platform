using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Repositories;

namespace Esports.Teams.Api.Services;

public interface IJugadorService
{
    Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname);
    Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais);
}

public class JugadorService : IJugadorService
{
    private readonly IJugadorRepository _repo;

    public JugadorService(IJugadorRepository repo) => _repo = repo;

    public Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname) =>
        _repo.ObtenerPorNicknameAsync(nickname);

    public Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais) =>
        _repo.ObtenerPorPaisAsync(pais);
}
