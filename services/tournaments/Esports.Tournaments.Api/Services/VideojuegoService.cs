using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Repositories;

namespace Esports.Tournaments.Api.Services;

public interface IVideojuegoService
{
    Task<VideojuegoResponse> CrearAsync(CrearVideojuegoRequest req);
    Task<VideojuegoResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<VideojuegoPorGeneroResponse>> ObtenerPorGeneroAsync(string genero);
    Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid videojuegoId);
}

public class VideojuegoService : IVideojuegoService
{
    private readonly IVideojuegoRepository _repo;

    public VideojuegoService(IVideojuegoRepository repo) => _repo = repo;

    public async Task<VideojuegoResponse> CrearAsync(CrearVideojuegoRequest req)
    {
        var v = new Videojuego
        {
            VideojuegoId = Guid.NewGuid(),
            Nombre = req.Nombre,
            Genero = req.Genero
        };
        await _repo.CrearAsync(v);
        return new VideojuegoResponse(v.VideojuegoId, v.Nombre, v.Genero);
    }

    public async Task<VideojuegoResponse?> ObtenerPorIdAsync(Guid id)
    {
        var v = await _repo.ObtenerPorIdAsync(id);
        return v is null ? null : new VideojuegoResponse(v.VideojuegoId, v.Nombre, v.Genero);
    }

    public Task<IEnumerable<VideojuegoPorGeneroResponse>> ObtenerPorGeneroAsync(string genero)
        => _repo.ObtenerPorGeneroAsync(genero);

    public Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid videojuegoId)
        => _repo.ObtenerTorneosAsync(videojuegoId);
}
