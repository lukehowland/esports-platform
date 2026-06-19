using Cassandra;
using Esports.Tournaments.Api.Cassandra;
using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;

namespace Esports.Tournaments.Api.Repositories;

public interface IVideojuegoRepository
{
    Task CrearAsync(Videojuego v);
    Task<Videojuego?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<VideojuegoPorGeneroResponse>> ObtenerPorGeneroAsync(string genero);
    Task<IEnumerable<TorneoPorVideojuegoResponse>> ObtenerTorneosAsync(Guid videojuegoId);
    Task<bool> TieneTorneosAsync(Guid videojuegoId);
    Task ActualizarAsync(Videojuego nuevo, string generoAnterior);
    Task EliminarAsync(Guid id, string genero);
}

public class VideojuegoRepository : IVideojuegoRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insBase, _insPorGenero;
    private readonly PreparedStatement _selById, _selPorGenero, _selTorneos;
    private readonly PreparedStatement _selTorneosLimit;
    private readonly PreparedStatement _updBase, _updPorGeneroNombre, _delPorGenero, _delBase;

    public VideojuegoRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";
        _insBase = _session.Prepare($"INSERT INTO {_ks}.videojuegos (videojuego_id, nombre, genero, plataforma) VALUES (?, ?, ?, ?)");
        _insPorGenero = _session.Prepare($"INSERT INTO {_ks}.videojuegos_por_genero (genero, videojuego_id, nombre, plataforma) VALUES (?, ?, ?, ?)");
        _selById = _session.Prepare($"SELECT videojuego_id, nombre, genero, plataforma FROM {_ks}.videojuegos WHERE videojuego_id = ?");
        _selPorGenero = _session.Prepare($"SELECT videojuego_id, nombre, plataforma FROM {_ks}.videojuegos_por_genero WHERE genero = ?");
        _selTorneos = _session.Prepare($"SELECT torneo_id, nombre_torneo, nombre_organizador, fecha_inicio FROM {_ks}.torneos_por_videojuego WHERE videojuego_id = ?");
        _selTorneosLimit = _session.Prepare($"SELECT torneo_id FROM {_ks}.torneos_por_videojuego WHERE videojuego_id = ? LIMIT 1");
        _updBase = _session.Prepare($"UPDATE {_ks}.videojuegos SET nombre = ?, genero = ?, plataforma = ? WHERE videojuego_id = ?");
        _updPorGeneroNombre = _session.Prepare($"UPDATE {_ks}.videojuegos_por_genero SET nombre = ?, plataforma = ? WHERE genero = ? AND videojuego_id = ?");
        _delPorGenero = _session.Prepare($"DELETE FROM {_ks}.videojuegos_por_genero WHERE genero = ? AND videojuego_id = ?");
        _delBase = _session.Prepare($"DELETE FROM {_ks}.videojuegos WHERE videojuego_id = ?");
    }

    public async Task CrearAsync(Videojuego v)
    {
        // BATCH: videojuegos + videojuegos_por_genero
        var batch = new BatchStatement();
        batch.Add(_insBase.Bind(v.VideojuegoId, v.Nombre, v.Genero, v.Plataforma));
        batch.Add(_insPorGenero.Bind(v.Genero, v.VideojuegoId, v.Nombre, v.Plataforma));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Videojuego?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        return new Videojuego
        {
            VideojuegoId = row.GetValue<Guid>("videojuego_id"),
            Nombre = row.GetValue<string>("nombre"),
            Genero = row.GetValue<string>("genero"),
            Plataforma = row.GetValue<string>("plataforma") ?? ""
        };
    }

    public async Task<IEnumerable<VideojuegoPorGeneroResponse>> ObtenerPorGeneroAsync(string genero)
    {
        var rows = await _session.ExecuteAsync(_selPorGenero.Bind(genero));
        return rows.Select(r => new VideojuegoPorGeneroResponse(
            r.GetValue<Guid>("videojuego_id"),
            r.GetValue<string>("nombre"),
            r.GetValue<string>("plataforma") ?? ""));
    }

    public async Task<IEnumerable<TorneoPorVideojuegoResponse>> ObtenerTorneosAsync(Guid videojuegoId)
    {
        var rows = await _session.ExecuteAsync(_selTorneos.Bind(videojuegoId));
        return rows.Select(r => new TorneoPorVideojuegoResponse(
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<string>("nombre_torneo"),
            r.GetValue<string>("nombre_organizador"),
            r.GetValue<DateTimeOffset>("fecha_inicio")));
    }

    public async Task<bool> TieneTorneosAsync(Guid videojuegoId)
    {
        var rows = await _session.ExecuteAsync(_selTorneosLimit.Bind(videojuegoId));
        return rows.Any();
    }

    public async Task ActualizarAsync(Videojuego nuevo, string generoAnterior)
    {
        // El género es parte de la primary key de videojuegos_por_genero (inmutable), así
        // que cambiar de género = borrar la fila vieja + insertar la nueva. Si el género no
        // cambia, basta con actualizar el nombre. Todo en un BATCH para mantener consistencia.
        var batch = new BatchStatement();
        batch.Add(_updBase.Bind(nuevo.Nombre, nuevo.Genero, nuevo.Plataforma, nuevo.VideojuegoId));

        if (string.Equals(nuevo.Genero, generoAnterior, StringComparison.Ordinal))
        {
            batch.Add(_updPorGeneroNombre.Bind(nuevo.Nombre, nuevo.Plataforma, nuevo.Genero, nuevo.VideojuegoId));
        }
        else
        {
            batch.Add(_delPorGenero.Bind(generoAnterior, nuevo.VideojuegoId));
            batch.Add(_insPorGenero.Bind(nuevo.Genero, nuevo.VideojuegoId, nuevo.Nombre, nuevo.Plataforma));
        }

        await _session.ExecuteAsync(batch);
    }

    public async Task EliminarAsync(Guid id, string genero)
    {
        var batch = new BatchStatement();
        batch.Add(_delBase.Bind(id));
        batch.Add(_delPorGenero.Bind(genero, id));
        await _session.ExecuteAsync(batch);
    }
}
