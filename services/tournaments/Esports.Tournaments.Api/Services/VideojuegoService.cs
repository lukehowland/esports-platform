using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Repositories;

namespace Esports.Tournaments.Api.Services;

public interface IVideojuegoService
{
    Task<VideojuegoResponse> CrearAsync(CrearVideojuegoRequest req);
    Task<VideojuegoResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<VideojuegoPorGeneroResponse>> ObtenerPorGeneroAsync(string genero);
    Task<IEnumerable<TorneoPorVideojuegoResponse>> ObtenerTorneosAsync(Guid videojuegoId);
    Task<(MutacionResultado Resultado, VideojuegoResponse? Videojuego)> ActualizarAsync(Guid id, EditarVideojuegoRequest req);
    Task<MutacionResultado> EliminarAsync(Guid id);
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
            Nombre = req.Nombre.Trim(),
            Genero = req.Genero.Trim().ToUpperInvariant()
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

    public Task<IEnumerable<TorneoPorVideojuegoResponse>> ObtenerTorneosAsync(Guid videojuegoId)
        => _repo.ObtenerTorneosAsync(videojuegoId);

    public async Task<(MutacionResultado Resultado, VideojuegoResponse? Videojuego)> ActualizarAsync(Guid id, EditarVideojuegoRequest req)
    {
        var actual = await _repo.ObtenerPorIdAsync(id);
        if (actual is null)
            return (MutacionResultado.NoEncontrado, null);

        // Block-on-dependents: el nombre se copia a las tablas de torneos; editar con
        // torneos asociados dejaría datos inconsistentes en otras particiones.
        if (await _repo.TieneTorneosAsync(id))
            return (MutacionResultado.ConDependencias, null);

        var nuevo = new Videojuego
        {
            VideojuegoId = id,
            Nombre = req.Nombre.Trim(),
            Genero = req.Genero.Trim().ToUpperInvariant(),
        };
        await _repo.ActualizarAsync(nuevo, actual.Genero);
        return (MutacionResultado.Ok, new VideojuegoResponse(nuevo.VideojuegoId, nuevo.Nombre, nuevo.Genero));
    }

    public async Task<MutacionResultado> EliminarAsync(Guid id)
    {
        var actual = await _repo.ObtenerPorIdAsync(id);
        if (actual is null)
            return MutacionResultado.NoEncontrado;

        if (await _repo.TieneTorneosAsync(id))
            return MutacionResultado.ConDependencias;

        await _repo.EliminarAsync(id, actual.Genero);
        return MutacionResultado.Ok;
    }
}
