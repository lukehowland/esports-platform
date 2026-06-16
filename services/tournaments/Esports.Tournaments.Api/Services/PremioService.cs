using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.HttpClients;
using Esports.Tournaments.Api.Repositories;

namespace Esports.Tournaments.Api.Services;

public interface IPremioService
{
    Task<PremioResponse> AsignarAsync(Guid torneoId, AsignarPremioRequest req);
    Task<IEnumerable<PremioResponse>> ObtenerPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<PremioEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId);
}

public class PremioService : IPremioService
{
    private readonly IPremioRepository _repo;
    private readonly ITorneoRepository _torneoRepo;
    private readonly TeamsClient _teamsClient;

    public PremioService(IPremioRepository repo, ITorneoRepository torneoRepo, TeamsClient teamsClient)
    {
        _repo = repo;
        _torneoRepo = torneoRepo;
        _teamsClient = teamsClient;
    }

    public async Task<PremioResponse> AsignarAsync(Guid torneoId, AsignarPremioRequest req)
    {
        var torneo = await _torneoRepo.ObtenerPorIdAsync(torneoId)
            ?? throw new KeyNotFoundException($"Torneo {torneoId} no encontrado.");

        string? nombreEquipo = null;
        if (req.EquipoId.HasValue)
        {
            var equipo = await _teamsClient.ObtenerEquipoAsync(req.EquipoId.Value)
                ?? throw new KeyNotFoundException($"Equipo {req.EquipoId} no encontrado.");
            nombreEquipo = equipo.Nombre;
        }

        var p = new Premio
        {
            PremioId = Guid.NewGuid(),
            TorneoId = torneoId,
            NombreTorneo = torneo.Nombre,
            Monto = req.Monto,
            Tipo = req.Tipo,
            EquipoId = req.EquipoId,
            NombreEquipo = nombreEquipo
        };
        await _repo.AsignarAsync(p);
        return new PremioResponse(p.PremioId, p.TorneoId, p.Monto, p.Tipo, p.EquipoId, p.NombreEquipo);
    }

    public Task<IEnumerable<PremioResponse>> ObtenerPorTorneoAsync(Guid torneoId)
        => _repo.ObtenerPorTorneoAsync(torneoId);

    public Task<IEnumerable<PremioEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId)
        => _repo.ObtenerPorEquipoAsync(equipoId);
}
