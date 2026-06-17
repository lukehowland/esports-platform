using Esports.Ranking.Api.Repositories;
using Esports.Shared.Events;
using MassTransit;

namespace Esports.Ranking.Api.Consumers;

public class TeamRegisteredConsumer : IConsumer<TeamRegisteredToTournament>
{
    private readonly IRankingRepository _repo;
    private readonly ILogger<TeamRegisteredConsumer> _logger;

    public TeamRegisteredConsumer(IRankingRepository repo, ILogger<TeamRegisteredConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TeamRegisteredToTournament> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Processing TeamRegisteredToTournament: equipo={Equipo}, torneo={Torneo}",
            evt.EquipoId, evt.TorneoId);

        // Q7: +1 torneo para el equipo
        await _repo.IncrementarTorneosEquipoAsync(evt.EquipoId);

        // Q23: +1 torneo para cada jugador del roster + guardar meta (nickname)
        foreach (var jugador in evt.Jugadores)
        {
            await _repo.IncrementarTorneosJugadorAsync(jugador.Id);
            await _repo.GuardarMetaJugadorAsync(jugador.Id, jugador.Nickname);
        }
    }
}
