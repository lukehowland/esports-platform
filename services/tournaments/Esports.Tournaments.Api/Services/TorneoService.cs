using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Repositories;

namespace Esports.Tournaments.Api.Services;

public interface ITorneoService
{
    Task<TorneoResponse> CrearAsync(CrearTorneoRequest req);
    Task<TorneoResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync();
    Task<TorneoPorCodigoResponse?> ObtenerPorCodigoAsync(string codigo);
}

public class TorneoService : ITorneoService
{
    private readonly ITorneoRepository _torneoRepo;
    private readonly IVideojuegoRepository _videojuegoRepo;
    private readonly IOrganizadorRepository _organizadorRepo;

    public TorneoService(ITorneoRepository torneoRepo, IVideojuegoRepository videojuegoRepo,
        IOrganizadorRepository organizadorRepo)
    {
        _torneoRepo = torneoRepo;
        _videojuegoRepo = videojuegoRepo;
        _organizadorRepo = organizadorRepo;
    }

    public async Task<TorneoResponse> CrearAsync(CrearTorneoRequest req)
    {
        var videojuego = await _videojuegoRepo.ObtenerPorIdAsync(req.VideojuegoId)
            ?? throw new KeyNotFoundException($"Videojuego {req.VideojuegoId} no encontrado.");
        var organizador = await _organizadorRepo.ObtenerPorIdAsync(req.OrganizadorId)
            ?? throw new KeyNotFoundException($"Organizador {req.OrganizadorId} no encontrado.");

        var t = new Torneo
        {
            TorneoId = Guid.NewGuid(),
            Nombre = req.Nombre,
            Codigo = req.Codigo,
            VideojuegoId = videojuego.VideojuegoId,
            NombreVideojuego = videojuego.Nombre,
            OrganizadorId = organizador.OrganizadorId,
            NombreOrganizador = organizador.Nombre,
            FechaInicio = req.FechaInicio
        };
        await _torneoRepo.CrearAsync(t);
        return new TorneoResponse(t.TorneoId, t.Nombre, t.Codigo, t.VideojuegoId, t.NombreVideojuego,
            t.OrganizadorId, t.NombreOrganizador, t.FechaInicio);
    }

    public async Task<TorneoResponse?> ObtenerPorIdAsync(Guid id)
    {
        var t = await _torneoRepo.ObtenerPorIdAsync(id);
        return t is null ? null : new TorneoResponse(t.TorneoId, t.Nombre, t.Codigo, t.VideojuegoId,
            t.NombreVideojuego, t.OrganizadorId, t.NombreOrganizador, t.FechaInicio);
    }

    public Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync()
        => _torneoRepo.ObtenerPorFechaAsync();

    public Task<TorneoPorCodigoResponse?> ObtenerPorCodigoAsync(string codigo)
        => _torneoRepo.ObtenerPorCodigoAsync(codigo);
}
