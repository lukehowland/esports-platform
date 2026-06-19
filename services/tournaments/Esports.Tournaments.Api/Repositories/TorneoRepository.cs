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
    Task<bool> TieneDependientesAsync(Guid torneoId);
    Task ActualizarAsync(Torneo original, string nuevoNombre, DateTimeOffset nuevaFechaFin);
    Task EliminarAsync(Torneo t);
}

public class TorneoRepository : ITorneoRepository
{
    private const string Bucket = "GLOBAL";

    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insBase, _insPorVideojuego, _insPorOrganizador, _insPorFecha, _insPorCodigo;
    private readonly PreparedStatement _selById, _selPorFecha, _selPorCodigo;
    private readonly PreparedStatement _selInscritosLimit, _selPremiosLimit;
    private readonly PreparedStatement _updBase, _updPorVideojuego, _updPorOrganizador, _updPorFecha, _updPorCodigo;
    private readonly PreparedStatement _delBase, _delPorVideojuego, _delPorOrganizador, _delPorFecha, _delPorCodigo;

    public TorneoRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";

        _insBase = _session.Prepare($@"INSERT INTO {_ks}.torneos
            (torneo_id, nombre, codigo, videojuego_id, nombre_videojuego, organizador_id, nombre_organizador, fecha_inicio, fecha_fin)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
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

        // Block-on-dependents: torneo con inscritos o premios no se edita/elimina (evita
        // copias desnormalizadas obsoletas en torneos_por_equipo y en partidas/premios).
        _selInscritosLimit = _session.Prepare($"SELECT equipo_id FROM {_ks}.equipos_por_torneo WHERE torneo_id = ? LIMIT 1");
        _selPremiosLimit = _session.Prepare($"SELECT premio_id FROM {_ks}.premios_por_torneo WHERE torneo_id = ? LIMIT 1");

        _updBase = _session.Prepare($"UPDATE {_ks}.torneos SET nombre = ?, fecha_fin = ? WHERE torneo_id = ?");
        _updPorVideojuego = _session.Prepare($"UPDATE {_ks}.torneos_por_videojuego SET nombre_torneo = ? WHERE videojuego_id = ? AND fecha_inicio = ? AND torneo_id = ?");
        _updPorOrganizador = _session.Prepare($"UPDATE {_ks}.torneos_por_organizador SET nombre_torneo = ? WHERE organizador_id = ? AND fecha_inicio = ? AND torneo_id = ?");
        _updPorFecha = _session.Prepare($"UPDATE {_ks}.torneos_por_fecha SET nombre_torneo = ? WHERE bucket = ? AND fecha_inicio = ? AND torneo_id = ?");
        _updPorCodigo = _session.Prepare($"UPDATE {_ks}.torneo_por_codigo SET nombre = ? WHERE codigo = ?");

        _delBase = _session.Prepare($"DELETE FROM {_ks}.torneos WHERE torneo_id = ?");
        _delPorVideojuego = _session.Prepare($"DELETE FROM {_ks}.torneos_por_videojuego WHERE videojuego_id = ? AND fecha_inicio = ? AND torneo_id = ?");
        _delPorOrganizador = _session.Prepare($"DELETE FROM {_ks}.torneos_por_organizador WHERE organizador_id = ? AND fecha_inicio = ? AND torneo_id = ?");
        _delPorFecha = _session.Prepare($"DELETE FROM {_ks}.torneos_por_fecha WHERE bucket = ? AND fecha_inicio = ? AND torneo_id = ?");
        _delPorCodigo = _session.Prepare($"DELETE FROM {_ks}.torneo_por_codigo WHERE codigo = ?");
    }

    public async Task CrearAsync(Torneo t)
    {
        // BATCH: torneos + torneos_por_videojuego + torneos_por_organizador + torneos_por_fecha + torneo_por_codigo
        var batch = new BatchStatement();
        batch.Add(_insBase.Bind(t.TorneoId, t.Nombre, t.Codigo, t.VideojuegoId, t.NombreVideojuego, t.OrganizadorId, t.NombreOrganizador, t.FechaInicio, t.FechaFin));
        batch.Add(_insPorVideojuego.Bind(t.VideojuegoId, t.FechaInicio, t.TorneoId, t.Nombre, t.NombreOrganizador));
        batch.Add(_insPorOrganizador.Bind(t.OrganizadorId, t.FechaInicio, t.TorneoId, t.Nombre, t.NombreVideojuego));
        batch.Add(_insPorFecha.Bind(Bucket, t.FechaInicio, t.TorneoId, t.Nombre, t.NombreVideojuego));
        batch.Add(_insPorCodigo.Bind(t.Codigo, t.TorneoId, t.Nombre, t.FechaInicio));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Torneo?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        var fechaInicio = row.GetValue<DateTimeOffset>("fecha_inicio");
        return new Torneo
        {
            TorneoId = row.GetValue<Guid>("torneo_id"),
            Nombre = row.GetValue<string>("nombre"),
            Codigo = row.GetValue<string>("codigo"),
            VideojuegoId = row.GetValue<Guid>("videojuego_id"),
            NombreVideojuego = row.GetValue<string>("nombre_videojuego"),
            OrganizadorId = row.GetValue<Guid>("organizador_id"),
            NombreOrganizador = row.GetValue<string>("nombre_organizador"),
            FechaInicio = fechaInicio,
            FechaFin = row.GetValue<DateTimeOffset?>("fecha_fin") ?? fechaInicio
        };
    }

    public async Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync()
    {
        var rows = await _session.ExecuteAsync(_selPorFecha.Bind(Bucket));
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

    public async Task<bool> TieneDependientesAsync(Guid torneoId)
    {
        var inscritos = await _session.ExecuteAsync(_selInscritosLimit.Bind(torneoId));
        if (inscritos.Any()) return true;
        var premios = await _session.ExecuteAsync(_selPremiosLimit.Bind(torneoId));
        return premios.Any();
    }

    public async Task ActualizarAsync(Torneo original, string nuevoNombre, DateTimeOffset nuevaFechaFin)
    {
        // Solo se edita cuando no hay dependientes, así que un BATCH a la base + las
        // desnormalizadas (claves desde el torneo original) mantiene la consistencia.
        var batch = new BatchStatement();
        batch.Add(_updBase.Bind(nuevoNombre, nuevaFechaFin, original.TorneoId));
        batch.Add(_updPorVideojuego.Bind(nuevoNombre, original.VideojuegoId, original.FechaInicio, original.TorneoId));
        batch.Add(_updPorOrganizador.Bind(nuevoNombre, original.OrganizadorId, original.FechaInicio, original.TorneoId));
        batch.Add(_updPorFecha.Bind(nuevoNombre, Bucket, original.FechaInicio, original.TorneoId));
        batch.Add(_updPorCodigo.Bind(nuevoNombre, original.Codigo));
        await _session.ExecuteAsync(batch);
    }

    public async Task EliminarAsync(Torneo t)
    {
        var batch = new BatchStatement();
        batch.Add(_delBase.Bind(t.TorneoId));
        batch.Add(_delPorVideojuego.Bind(t.VideojuegoId, t.FechaInicio, t.TorneoId));
        batch.Add(_delPorOrganizador.Bind(t.OrganizadorId, t.FechaInicio, t.TorneoId));
        batch.Add(_delPorFecha.Bind(Bucket, t.FechaInicio, t.TorneoId));
        batch.Add(_delPorCodigo.Bind(t.Codigo));
        await _session.ExecuteAsync(batch);
    }
}
