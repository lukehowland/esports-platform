namespace Esports.Shared.Events;

public record JugadorRef(Guid Id, string Nickname);

public record TeamRegisteredToTournament(
    Guid EquipoId,
    Guid TorneoId,
    string NombreEquipo,
    IReadOnlyList<JugadorRef> Jugadores,
    DateTimeOffset FechaInscripcion)
{
    public IReadOnlyList<Guid> JugadorIds => Jugadores.Select(j => j.Id).ToList();
}
