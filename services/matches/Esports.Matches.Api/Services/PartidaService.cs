using Esports.Matches.Api.Domain;
using Esports.Matches.Api.Dtos;
using Esports.Matches.Api.Repositories;
using Esports.Shared.Events;
using MassTransit;

namespace Esports.Matches.Api.Services;

public interface IPartidaService
{
    Task<PartidaResponse> RegistrarAsync(RegistrarPartidaRequest req);
    Task<PartidaResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<PartidaPorTorneoResponse>> ObtenerPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<PartidaPorEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId);
    Task<IEnumerable<PartidaPorFechaResponse>> ObtenerPorFechaAsync(string dia);
    Task<IEnumerable<PartidaPorRivalesResponse>> ObtenerPorRivalesAsync(Guid equipoId, Guid rivalId);
}

public class PartidaService : IPartidaService
{
    private readonly IPartidaRepository _repo;
    private readonly IPublishEndpoint _bus;

    public PartidaService(IPartidaRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus = bus;
    }

    public async Task<PartidaResponse> RegistrarAsync(RegistrarPartidaRequest req)
    {
        var p = new Partida
        {
            PartidaId = Guid.NewGuid(),
            TorneoId = req.TorneoId,
            NombreTorneo = req.NombreTorneo,
            Fecha = req.Fecha,
            EquipoLocalId = req.EquipoLocalId,
            EquipoVisitanteId = req.EquipoVisitanteId,
            NombreLocal = req.NombreLocal,
            NombreVisitante = req.NombreVisitante,
            EquipoGanadorId = req.EquipoGanadorId,
            Resultado = req.Resultado
        };

        await _repo.RegistrarAsync(p);

        await _bus.Publish(new MatchPlayed(
            p.PartidaId, p.TorneoId,
            p.EquipoLocalId, p.EquipoVisitanteId, p.EquipoGanadorId,
            p.Fecha));

        return new PartidaResponse(p.PartidaId, p.TorneoId, p.NombreTorneo, p.Fecha,
            p.EquipoLocalId, p.EquipoVisitanteId, p.NombreLocal, p.NombreVisitante,
            p.EquipoGanadorId, p.Resultado);
    }

    public Task<PartidaResponse?> ObtenerPorIdAsync(Guid id)
        => _repo.ObtenerPorIdAsync(id);

    public Task<IEnumerable<PartidaPorTorneoResponse>> ObtenerPorTorneoAsync(Guid torneoId)
        => _repo.ObtenerPorTorneoAsync(torneoId);

    public Task<IEnumerable<PartidaPorEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId)
        => _repo.ObtenerPorEquipoAsync(equipoId);

    public async Task<IEnumerable<PartidaPorFechaResponse>> ObtenerPorFechaAsync(string dia)
    {
        var parts = dia.Split('-');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out var y) ||
            !int.TryParse(parts[1], out var m) ||
            !int.TryParse(parts[2], out var d))
            throw new ArgumentException($"Formato de fecha inválido: '{dia}'. Use YYYY-MM-DD.");

        var localDate = new global::Cassandra.LocalDate(y, m, d);
        return await _repo.ObtenerPorFechaAsync(localDate);
    }

    public Task<IEnumerable<PartidaPorRivalesResponse>> ObtenerPorRivalesAsync(Guid equipoId, Guid rivalId)
        => _repo.ObtenerPorRivalesAsync(equipoId, rivalId);
}
