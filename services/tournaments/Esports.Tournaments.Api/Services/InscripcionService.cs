using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.HttpClients;
using Esports.Tournaments.Api.Repositories;
using MassTransit;
using Esports.Shared.Events;

namespace Esports.Tournaments.Api.Services;

public interface IInscripcionService
{
    Task InscribirAsync(Guid torneoId, InscribirEquipoRequest req);
    Task<IEnumerable<EquipoPorTorneoResponse>> ObtenerEquiposPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<TorneoPorEquipoResponse>> ObtenerTorneosPorEquipoAsync(Guid equipoId);
}

public class InscripcionService : IInscripcionService
{
    private readonly IInscripcionRepository _repo;
    private readonly ITorneoRepository _torneoRepo;
    private readonly TeamsClient _teamsClient;
    private readonly IPublishEndpoint _bus;

    public InscripcionService(IInscripcionRepository repo, ITorneoRepository torneoRepo,
        TeamsClient teamsClient, IPublishEndpoint bus)
    {
        _repo = repo;
        _torneoRepo = torneoRepo;
        _teamsClient = teamsClient;
        _bus = bus;
    }

    public async Task InscribirAsync(Guid torneoId, InscribirEquipoRequest req)
    {
        var torneo = await _torneoRepo.ObtenerPorIdAsync(torneoId)
            ?? throw new KeyNotFoundException($"Torneo {torneoId} no encontrado.");

        var equipo = await _teamsClient.ObtenerEquipoAsync(req.EquipoId)
            ?? throw new KeyNotFoundException($"Equipo {req.EquipoId} no encontrado en el servicio teams.");

        var jugadorIds = await _teamsClient.ObtenerJugadorIdsAsync(req.EquipoId);

        var fechaInscripcion = DateTimeOffset.UtcNow;
        await _repo.InscribirAsync(torneoId, req.EquipoId, equipo.Nombre,
            fechaInscripcion, torneo.FechaInicio, torneo.Nombre, torneo.NombreVideojuego);

        await _bus.Publish(new TeamRegisteredToTournament(
            req.EquipoId, torneoId, equipo.Nombre, jugadorIds, fechaInscripcion));
    }

    public Task<IEnumerable<EquipoPorTorneoResponse>> ObtenerEquiposPorTorneoAsync(Guid torneoId)
        => _repo.ObtenerEquiposPorTorneoAsync(torneoId);

    public Task<IEnumerable<TorneoPorEquipoResponse>> ObtenerTorneosPorEquipoAsync(Guid equipoId)
        => _repo.ObtenerTorneosPorEquipoAsync(equipoId);
}
