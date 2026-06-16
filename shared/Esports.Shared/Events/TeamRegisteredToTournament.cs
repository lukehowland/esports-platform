namespace Esports.Shared.Events;

public record TeamRegisteredToTournament(
    Guid EquipoId,
    Guid TorneoId,
    string NombreEquipo,
    IReadOnlyList<Guid> JugadorIds,
    DateTimeOffset FechaInscripcion);
