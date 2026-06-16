using Cassandra;
using Esports.Tournaments.Api.Cassandra;
using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;

namespace Esports.Tournaments.Api.Repositories;

public interface ITorneoRepository
{
    Task CrearAsync(Torneo t);
    Task<Torneo?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync();
    Task<TorneoPorCodigoResponse?> ObtenerPorCodigoAsync(string codigo);
}

public class TorneoRepository : ITorneoRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insBase, _insPorVideojuego, _insPorOrganizador, _insPorFecha, _insPorCodigo;
    private readonly PreparedStatement _selById, _selPorFecha, _selPorCodigo;

    public TorneoRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";

        _insBase = _session.Prepare($@"INSERT INTO {_ks}.torneos
            (torneo_id, nombre, codigo, videojuego_id, nombre_videojuego, organizador_id, nombre_organizador, fecha_inicio)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
        _insPorVideojuego = _session.Prepare($@"INSERT INTO {_ks}.torneos_por_videojuego
            (videojuego_id, fecha_inicio, torneo_id, nombre_torneo, nombre_organizador)
            VALUES (?, ?, ?, ?, ?)");
        _insPorOrganizador = _session.Prepare($@"INSERT INTO {_ks}.torneos_por_organizador
            (organizador_id, fecha_inicio, torneo_id, nombre_torneo, nombre_videojuego)
            VALUES (?, ?, ?, ?, ?)");
        _insPorFecha = _session.Prepare($@"INSERT INTO {_ks}.torneos_por_fecha
            (bucket, fecha_inicio, torneo_id, nombre_torneo, nombre_videojuego)
            VALUES (?, ?, ?, ?, ?)");
        _insPorCodigo = _session.Prepare($@"INSERT INTO {_ks}.torneo_por_codigo
            (codigo, torneo_id, nombre, fecha_inicio)
            VALUES (?, ?, ?, ?)");

        _selById = _session.Prepare($"SELECT * FROM {_ks}.torneos WHERE torneo_id = ?");
        _selPorFecha = _session.Prepare($"SELECT torneo_id, nombre_torneo, nombre_videojuego, fecha_inicio FROM {_ks}.torneos_por_fecha WHERE bucket = ?");
        _selPorCodigo = _session.Prepare($"SELECT torneo_id, nombre, fecha_inicio FROM {_ks}.torneo_por_codigo WHERE codigo = ?");
    }

    public async Task CrearAsync(Torneo t)
    {
        // BATCH: torneos + torneos_por_videojuego + torneos_por_organizador + torneos_por_fecha + torneo_por_codigo
        var batch = new BatchStatement();
        batch.Add(_insBase.Bind(t.TorneoId, t.Nombre, t.Codigo, t.VideojuegoId, t.NombreVideojuego, t.OrganizadorId, t.NombreOrganizador, t.FechaInicio));
        batch.Add(_insPorVideojuego.Bind(t.VideojuegoId, t.FechaInicio, t.TorneoId, t.Nombre, t.NombreOrganizador));
        batch.Add(_insPorOrganizador.Bind(t.OrganizadorId, t.FechaInicio, t.TorneoId, t.Nombre, t.NombreVideojuego));
        batch.Add(_insPorFecha.Bind("GLOBAL", t.FechaInicio, t.TorneoId, t.Nombre, t.NombreVideojuego));
        batch.Add(_insPorCodigo.Bind(t.Codigo, t.TorneoId, t.Nombre, t.FechaInicio));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Torneo?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        return new Torneo
        {
            TorneoId = row.GetValue<Guid>("torneo_id"),
            Nombre = row.GetValue<string>("nombre"),
            Codigo = row.GetValue<string>("codigo"),
            VideojuegoId = row.GetValue<Guid>("videojuego_id"),
            NombreVideojuego = row.GetValue<string>("nombre_videojuego"),
            OrganizadorId = row.GetValue<Guid>("organizador_id"),
            NombreOrganizador = row.GetValue<string>("nombre_organizador"),
            FechaInicio = row.GetValue<DateTimeOffset>("fecha_inicio")
        };
    }

    public async Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync()
    {
        var rows = await _session.ExecuteAsync(_selPorFecha.Bind("GLOBAL"));
        return rows.Select(r => new TorneoResumenResponse(
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<string>("nombre_torneo"),
            r.GetValue<string>("nombre_videojuego"),
            r.GetValue<DateTimeOffset>("fecha_inicio")));
    }

    public async Task<TorneoPorCodigoResponse?> ObtenerPorCodigoAsync(string codigo)
    {
        var row = (await _session.ExecuteAsync(_selPorCodigo.Bind(codigo))).FirstOrDefault();
        if (row is null) return null;
        return new TorneoPorCodigoResponse(
            row.GetValue<Guid>("torneo_id"),
            row.GetValue<string>("nombre"),
            row.GetValue<DateTimeOffset>("fecha_inicio"));
    }
}
