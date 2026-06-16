namespace Esports.Shared.Events;

public record MatchPlayed(
    Guid PartidaId,
    Guid TorneoId,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    Guid EquipoGanadorId,
    DateTimeOffset Fecha);
