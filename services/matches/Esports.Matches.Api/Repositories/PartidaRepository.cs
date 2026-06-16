using Cassandra;
using Esports.Matches.Api.Cassandra;
using Esports.Matches.Api.Domain;
using Esports.Matches.Api.Dtos;

namespace Esports.Matches.Api.Repositories;

public interface IPartidaRepository
{
    Task RegistrarAsync(Partida p);
    Task<PartidaResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<PartidaPorTorneoResponse>> ObtenerPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<PartidaPorEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId);
    Task<IEnumerable<PartidaPorFechaResponse>> ObtenerPorFechaAsync(global::Cassandra.LocalDate dia);
    Task<IEnumerable<PartidaPorRivalesResponse>> ObtenerPorRivalesAsync(Guid equipoId, Guid rivalId);
}

public class PartidaRepository : IPartidaRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;

    private readonly PreparedStatement _insBase;
    private readonly PreparedStatement _insPorTorneo;
    private readonly PreparedStatement _insPorEquipo;
    private readonly PreparedStatement _insPorFecha;
    private readonly PreparedStatement _insPorRivales;

    private readonly PreparedStatement _selById;
    private readonly PreparedStatement _selPorTorneo;
    private readonly PreparedStatement _selPorEquipo;
    private readonly PreparedStatement _selPorFecha;
    private readonly PreparedStatement _selPorRivales;

    public PartidaRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_matches";

        _insBase = _session.Prepare($@"INSERT INTO {_ks}.partidas
            (partida_id, torneo_id, nombre_torneo, fecha, dia,
             equipo_local_id, equipo_visitante_id, nombre_local, nombre_visitante,
             equipo_ganador_id, resultado)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");

        _insPorTorneo = _session.Prepare($@"INSERT INTO {_ks}.partidas_por_torneo
            (torneo_id, fecha, partida_id, nombre_local, nombre_visitante, resultado)
            VALUES (?, ?, ?, ?, ?, ?)");

        _insPorEquipo = _session.Prepare($@"INSERT INTO {_ks}.partidas_por_equipo
            (equipo_id, fecha, partida_id, nombre_torneo, rival, resultado)
            VALUES (?, ?, ?, ?, ?, ?)");

        _insPorFecha = _session.Prepare($@"INSERT INTO {_ks}.partidas_por_fecha
            (dia, partida_id, torneo_id, nombre_local, nombre_visitante, resultado)
            VALUES (?, ?, ?, ?, ?, ?)");

        _insPorRivales = _session.Prepare($@"INSERT INTO {_ks}.partidas_por_rivales
            (equipo_id, rival_id, fecha, partida_id, equipo_local_id, resultado)
            VALUES (?, ?, ?, ?, ?, ?)");

        _selById = _session.Prepare($"SELECT * FROM {_ks}.partidas WHERE partida_id = ?");
        _selPorTorneo = _session.Prepare($"SELECT fecha, partida_id, nombre_local, nombre_visitante, resultado FROM {_ks}.partidas_por_torneo WHERE torneo_id = ?");
        _selPorEquipo = _session.Prepare($"SELECT fecha, partida_id, nombre_torneo, rival, resultado FROM {_ks}.partidas_por_equipo WHERE equipo_id = ?");
        _selPorFecha = _session.Prepare($"SELECT partida_id, torneo_id, nombre_local, nombre_visitante, resultado FROM {_ks}.partidas_por_fecha WHERE dia = ?");
        _selPorRivales = _session.Prepare($"SELECT fecha, partida_id, equipo_local_id, resultado FROM {_ks}.partidas_por_rivales WHERE equipo_id = ? AND rival_id = ?");
    }

    public async Task RegistrarAsync(Partida p)
    {
        var dia = new global::Cassandra.LocalDate(p.Fecha.Year, p.Fecha.Month, p.Fecha.Day);
        var resultadoLocal = p.EquipoGanadorId == p.EquipoLocalId ? "VICTORIA" : "DERROTA";
        var resultadoVisitante = p.EquipoGanadorId == p.EquipoVisitanteId ? "VICTORIA" : "DERROTA";

        // BATCH: 7 filas en 5 tablas (Fix 2 y Fix 3)
        var batch = new BatchStatement();
        // Base
        batch.Add(_insBase.Bind(p.PartidaId, p.TorneoId, p.NombreTorneo, p.Fecha, dia,
            p.EquipoLocalId, p.EquipoVisitanteId, p.NombreLocal, p.NombreVisitante,
            p.EquipoGanadorId, p.Resultado));
        // Q16
        batch.Add(_insPorTorneo.Bind(p.TorneoId, p.Fecha, p.PartidaId, p.NombreLocal, p.NombreVisitante, p.Resultado));
        // Q17 (2 filas)
        batch.Add(_insPorEquipo.Bind(p.EquipoLocalId, p.Fecha, p.PartidaId, p.NombreTorneo, p.NombreVisitante, resultadoLocal));
        batch.Add(_insPorEquipo.Bind(p.EquipoVisitanteId, p.Fecha, p.PartidaId, p.NombreTorneo, p.NombreLocal, resultadoVisitante));
        // Q18 (Fix 3: dia = date)
        batch.Add(_insPorFecha.Bind(dia, p.PartidaId, p.TorneoId, p.NombreLocal, p.NombreVisitante, p.Resultado));
        // Q19 (Fix 2: bidireccional, 2 filas)
        batch.Add(_insPorRivales.Bind(p.EquipoLocalId, p.EquipoVisitanteId, p.Fecha, p.PartidaId, p.EquipoLocalId, p.Resultado));
        batch.Add(_insPorRivales.Bind(p.EquipoVisitanteId, p.EquipoLocalId, p.Fecha, p.PartidaId, p.EquipoLocalId, p.Resultado));

        await _session.ExecuteAsync(batch);
    }

    public async Task<PartidaResponse?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        return new PartidaResponse(
            row.GetValue<Guid>("partida_id"),
            row.GetValue<Guid>("torneo_id"),
            row.GetValue<string>("nombre_torneo"),
            row.GetValue<DateTimeOffset>("fecha"),
            row.GetValue<Guid>("equipo_local_id"),
            row.GetValue<Guid>("equipo_visitante_id"),
            row.GetValue<string>("nombre_local"),
            row.GetValue<string>("nombre_visitante"),
            row.GetValue<Guid>("equipo_ganador_id"),
            row.GetValue<string>("resultado"));
    }

    public async Task<IEnumerable<PartidaPorTorneoResponse>> ObtenerPorTorneoAsync(Guid torneoId)
    {
        var rows = await _session.ExecuteAsync(_selPorTorneo.Bind(torneoId));
        return rows.Select(r => new PartidaPorTorneoResponse(
            r.GetValue<Guid>("partida_id"),
            r.GetValue<string>("nombre_local"),
            r.GetValue<string>("nombre_visitante"),
            r.GetValue<string>("resultado"),
            r.GetValue<DateTimeOffset>("fecha")));
    }

    public async Task<IEnumerable<PartidaPorEquipoResponse>> ObtenerPorEquipoAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selPorEquipo.Bind(equipoId));
        return rows.Select(r => new PartidaPorEquipoResponse(
            r.GetValue<Guid>("partida_id"),
            r.GetValue<string>("nombre_torneo"),
            r.GetValue<string>("rival"),
            r.GetValue<string>("resultado"),
            r.GetValue<DateTimeOffset>("fecha")));
    }

    public async Task<IEnumerable<PartidaPorFechaResponse>> ObtenerPorFechaAsync(global::Cassandra.LocalDate dia)
    {
        var rows = await _session.ExecuteAsync(_selPorFecha.Bind(dia));
        return rows.Select(r => new PartidaPorFechaResponse(
            r.GetValue<Guid>("partida_id"),
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<string>("nombre_local"),
            r.GetValue<string>("nombre_visitante"),
            r.GetValue<string>("resultado")));
    }

    public async Task<IEnumerable<PartidaPorRivalesResponse>> ObtenerPorRivalesAsync(Guid equipoId, Guid rivalId)
    {
        var rows = await _session.ExecuteAsync(_selPorRivales.Bind(equipoId, rivalId));
        return rows.Select(r => new PartidaPorRivalesResponse(
            r.GetValue<Guid>("partida_id"),
            r.GetValue<Guid>("equipo_local_id"),
            r.GetValue<string>("resultado"),
            r.GetValue<DateTimeOffset>("fecha")));
    }
}
