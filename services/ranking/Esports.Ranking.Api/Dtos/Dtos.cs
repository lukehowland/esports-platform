namespace Esports.Ranking.Api.Dtos;

// Q7: ranking global de equipos por torneos
public record RankingEquipoResponse(int Posicion, Guid EquipoId, long TotalTorneos);

// Q22: ranking de equipos por victorias
public record RankingVictoriasResponse(int Posicion, Guid EquipoId, long TotalVictorias);

// Q23: jugadores más activos
public record RankingJugadorResponse(int Posicion, Guid JugadorId, long TotalTorneos, string? NombreJugador = null);

// Q24: estadísticas de un equipo en un torneo
public record StatsEquipoTorneoResponse(
    Guid EquipoId,
    Guid TorneoId,
    long Victorias,
    long Derrotas,
    long PartidasJugadas);
