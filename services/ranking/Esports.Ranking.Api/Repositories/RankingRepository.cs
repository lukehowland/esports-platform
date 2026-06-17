using Cassandra;
using Esports.Ranking.Api.Cassandra;
using Esports.Ranking.Api.Dtos;

namespace Esports.Ranking.Api.Repositories;

public interface IRankingRepository
{
    // Escrituras (solo UPDATE counter, nunca INSERT)
    Task IncrementarTorneosEquipoAsync(Guid equipoId);
    Task IncrementarTorneosJugadorAsync(Guid jugadorId);
    Task GuardarMetaJugadorAsync(Guid jugadorId, string nickname);
    Task IncrementarVictoriasAsync(Guid equipoGanadorId);
    Task ActualizarStatsPartidaAsync(Guid torneoId, Guid equipoGanadorId, Guid equipoPerdedorId);

    // Lecturas
    Task<IEnumerable<RankingEquipoResponse>> ObtenerRankingEquiposAsync(int top);
    Task<IEnumerable<RankingVictoriasResponse>> ObtenerRankingVictoriasAsync(int top);
    Task<IEnumerable<RankingJugadorResponse>> ObtenerRankingJugadoresAsync(int top);
    Task<StatsEquipoTorneoResponse?> ObtenerStatsEquipoTorneoAsync(Guid equipoId, Guid torneoId);
}

public class RankingRepository : IRankingRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;

    public RankingRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_ranking";
    }

    public async Task IncrementarTorneosEquipoAsync(Guid equipoId)
    {
        // Counter: ONLY UPDATE, never INSERT
        await _session.ExecuteAsync(new SimpleStatement(
            $"UPDATE {_ks}.ranking_equipos_global SET total_torneos = total_torneos + 1 WHERE bucket = 'GLOBAL' AND equipo_id = ?",
            equipoId));
    }

    public async Task IncrementarTorneosJugadorAsync(Guid jugadorId)
    {
        await _session.ExecuteAsync(new SimpleStatement(
            $"UPDATE {_ks}.ranking_jugadores_activos SET total_torneos = total_torneos + 1 WHERE bucket = 'GLOBAL' AND jugador_id = ?",
            jugadorId));
    }

    public async Task GuardarMetaJugadorAsync(Guid jugadorId, string nickname)
    {
        await _session.ExecuteAsync(new SimpleStatement(
            $"INSERT INTO {_ks}.ranking_jugadores_meta (jugador_id, nickname) VALUES (?, ?)",
            jugadorId, nickname));
    }

    public async Task IncrementarVictoriasAsync(Guid equipoGanadorId)
    {
        await _session.ExecuteAsync(new SimpleStatement(
            $"UPDATE {_ks}.ranking_victorias SET total_victorias = total_victorias + 1 WHERE bucket = 'GLOBAL' AND equipo_id = ?",
            equipoGanadorId));
    }

    public async Task ActualizarStatsPartidaAsync(Guid torneoId, Guid equipoGanadorId, Guid equipoPerdedorId)
    {
        // Ganador: +1 victorias, +1 partidas_jugadas
        await _session.ExecuteAsync(new SimpleStatement(
            $"UPDATE {_ks}.stats_equipo_por_torneo SET victorias = victorias + 1, partidas_jugadas = partidas_jugadas + 1 WHERE equipo_id = ? AND torneo_id = ?",
            equipoGanadorId, torneoId));
        // Perdedor: +1 derrotas, +1 partidas_jugadas
        await _session.ExecuteAsync(new SimpleStatement(
            $"UPDATE {_ks}.stats_equipo_por_torneo SET derrotas = derrotas + 1, partidas_jugadas = partidas_jugadas + 1 WHERE equipo_id = ? AND torneo_id = ?",
            equipoPerdedorId, torneoId));
    }

    public async Task<IEnumerable<RankingEquipoResponse>> ObtenerRankingEquiposAsync(int top)
    {
        var rows = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT equipo_id, total_torneos FROM {_ks}.ranking_equipos_global WHERE bucket = 'GLOBAL'"));
        return rows
            .Select(r => new RankingEquipoResponse(r.GetValue<Guid>("equipo_id"), r.GetValue<long?>("total_torneos") ?? 0L))
            .OrderByDescending(x => x.TotalTorneos)
            .Take(top);
    }

    public async Task<IEnumerable<RankingVictoriasResponse>> ObtenerRankingVictoriasAsync(int top)
    {
        var rows = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT equipo_id, total_victorias FROM {_ks}.ranking_victorias WHERE bucket = 'GLOBAL'"));
        return rows
            .Select(r => new RankingVictoriasResponse(r.GetValue<Guid>("equipo_id"), r.GetValue<long?>("total_victorias") ?? 0L))
            .OrderByDescending(x => x.TotalVictorias)
            .Take(top);
    }

    public async Task<IEnumerable<RankingJugadorResponse>> ObtenerRankingJugadoresAsync(int top)
    {
        var rows = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT jugador_id, total_torneos FROM {_ks}.ranking_jugadores_activos WHERE bucket = 'GLOBAL'"));

        var ranking = rows
            .Select(r => new { JugadorId = r.GetValue<Guid>("jugador_id"), TotalTorneos = r.GetValue<long?>("total_torneos") ?? 0L })
            .OrderByDescending(x => x.TotalTorneos)
            .Take(top)
            .ToList();

        if (ranking.Count == 0) return [];

        // Resolver nicknames desde la tabla meta (plain table, no counter)
        var ids = ranking.Select(x => x.JugadorId).ToList();
        var metaRows = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT jugador_id, nickname FROM {_ks}.ranking_jugadores_meta WHERE jugador_id IN ?", ids));
        var nicknames = metaRows.ToDictionary(r => r.GetValue<Guid>("jugador_id"), r => r.GetValue<string?>("nickname"));

        return ranking.Select(x => new RankingJugadorResponse(
            x.JugadorId,
            x.TotalTorneos,
            nicknames.GetValueOrDefault(x.JugadorId)));
    }

    public async Task<StatsEquipoTorneoResponse?> ObtenerStatsEquipoTorneoAsync(Guid equipoId, Guid torneoId)
    {
        var row = (await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT equipo_id, torneo_id, victorias, derrotas, partidas_jugadas FROM {_ks}.stats_equipo_por_torneo WHERE equipo_id = ? AND torneo_id = ?",
            equipoId, torneoId))).FirstOrDefault();
        if (row is null) return null;
        return new StatsEquipoTorneoResponse(
            row.GetValue<Guid>("equipo_id"),
            row.GetValue<Guid>("torneo_id"),
            row.GetValue<long?>("victorias") ?? 0L,
            row.GetValue<long?>("derrotas") ?? 0L,
            row.GetValue<long?>("partidas_jugadas") ?? 0L);
    }
}
