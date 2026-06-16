using Cassandra;
using Esports.Tournaments.Api.Cassandra;
using Esports.Tournaments.Api.Dtos;

namespace Esports.Tournaments.Api.Repositories;

public interface IInscripcionRepository
{
    Task InscribirAsync(Guid torneoId, Guid equipoId, string nombreEquipo,
        DateTimeOffset fechaInscripcion, DateTimeOffset fechaInicioTorneo,
        string nombreTorneo, string nombreVideojuego);
    Task<IEnumerable<EquipoPorTorneoResponse>> ObtenerEquiposPorTorneoAsync(Guid torneoId);
    Task<IEnumerable<TorneoPorEquipoResponse>> ObtenerTorneosPorEquipoAsync(Guid equipoId);
}

public class InscripcionRepository : IInscripcionRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insEquiposPorTorneo, _insTorneosPorEquipo;
    private readonly PreparedStatement _selEquiposPorTorneo, _selTorneosPorEquipo;

    public InscripcionRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";
        _insEquiposPorTorneo = _session.Prepare($@"INSERT INTO {_ks}.equipos_por_torneo
            (torneo_id, equipo_id, nombre_equipo, fecha_inscripcion) VALUES (?, ?, ?, ?)");
        _insTorneosPorEquipo = _session.Prepare($@"INSERT INTO {_ks}.torneos_por_equipo
            (equipo_id, fecha_inicio, torneo_id, nombre_torneo, nombre_videojuego) VALUES (?, ?, ?, ?, ?)");
        _selEquiposPorTorneo = _session.Prepare($"SELECT equipo_id, nombre_equipo, fecha_inscripcion FROM {_ks}.equipos_por_torneo WHERE torneo_id = ?");
        _selTorneosPorEquipo = _session.Prepare($"SELECT torneo_id, nombre_torneo, nombre_videojuego, fecha_inicio FROM {_ks}.torneos_por_equipo WHERE equipo_id = ?");
    }

    public async Task InscribirAsync(Guid torneoId, Guid equipoId, string nombreEquipo,
        DateTimeOffset fechaInscripcion, DateTimeOffset fechaInicioTorneo,
        string nombreTorneo, string nombreVideojuego)
    {
        // BATCH: equipos_por_torneo + torneos_por_equipo
        var batch = new BatchStatement();
        batch.Add(_insEquiposPorTorneo.Bind(torneoId, equipoId, nombreEquipo, fechaInscripcion));
        batch.Add(_insTorneosPorEquipo.Bind(equipoId, fechaInicioTorneo, torneoId, nombreTorneo, nombreVideojuego));
        await _session.ExecuteAsync(batch);
    }

    public async Task<IEnumerable<EquipoPorTorneoResponse>> ObtenerEquiposPorTorneoAsync(Guid torneoId)
    {
        var rows = await _session.ExecuteAsync(_selEquiposPorTorneo.Bind(torneoId));
        return rows.Select(r => new EquipoPorTorneoResponse(
            r.GetValue<Guid>("equipo_id"),
            r.GetValue<string>("nombre_equipo"),
            r.GetValue<DateTimeOffset>("fecha_inscripcion")));
    }

    public async Task<IEnumerable<TorneoPorEquipoResponse>> ObtenerTorneosPorEquipoAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selTorneosPorEquipo.Bind(equipoId));
        return rows.Select(r => new TorneoPorEquipoResponse(
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<string>("nombre_torneo"),
            r.GetValue<string>("nombre_videojuego"),
            r.GetValue<DateTimeOffset>("fecha_inicio")));
    }
}
