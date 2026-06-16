using Esports.Ranking.Api.Repositories;
using Esports.Shared.Events;
using MassTransit;

namespace Esports.Ranking.Api.Consumers;

public class MatchPlayedConsumer : IConsumer<MatchPlayed>
{
    private readonly IRankingRepository _repo;
    private readonly ILogger<MatchPlayedConsumer> _logger;

    public MatchPlayedConsumer(IRankingRepository repo, ILogger<MatchPlayedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MatchPlayed> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Processing MatchPlayed: partida={Partida}, ganador={Ganador}",
            evt.PartidaId, evt.EquipoGanadorId);

        // Q22: +1 victoria para el ganador
        await _repo.IncrementarVictoriasAsync(evt.EquipoGanadorId);

        // Q24: actualizar stats de ganador y perdedor en ese torneo
        var perdedorId = evt.EquipoGanadorId == evt.EquipoLocalId
            ? evt.EquipoVisitanteId
            : evt.EquipoLocalId;

        await _repo.ActualizarStatsPartidaAsync(evt.TorneoId, evt.EquipoGanadorId, perdedorId);
    }
}
