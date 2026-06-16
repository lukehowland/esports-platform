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
}

public class VideojuegoRepository : IVideojuegoRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insBase, _insPorGenero;
    private readonly PreparedStatement _selById, _selPorGenero, _selTorneos;

    public VideojuegoRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";
        _insBase = _session.Prepare($"INSERT INTO {_ks}.videojuegos (videojuego_id, nombre, genero) VALUES (?, ?, ?)");
        _insPorGenero = _session.Prepare($"INSERT INTO {_ks}.videojuegos_por_genero (genero, videojuego_id, nombre) VALUES (?, ?, ?)");
        _selById = _session.Prepare($"SELECT videojuego_id, nombre, genero FROM {_ks}.videojuegos WHERE videojuego_id = ?");
        _selPorGenero = _session.Prepare($"SELECT videojuego_id, nombre FROM {_ks}.videojuegos_por_genero WHERE genero = ?");
        _selTorneos = _session.Prepare($"SELECT torneo_id, nombre_torneo, nombre_organizador, fecha_inicio FROM {_ks}.torneos_por_videojuego WHERE videojuego_id = ?");
    }

    public async Task CrearAsync(Videojuego v)
    {
        // BATCH: videojuegos + videojuegos_por_genero
        var batch = new BatchStatement();
        batch.Add(_insBase.Bind(v.VideojuegoId, v.Nombre, v.Genero));
        batch.Add(_insPorGenero.Bind(v.Genero, v.VideojuegoId, v.Nombre));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Videojuego?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        return new Videojuego { VideojuegoId = row.GetValue<Guid>("videojuego_id"), Nombre = row.GetValue<string>("nombre"), Genero = row.GetValue<string>("genero") };
    }

    public async Task<IEnumerable<VideojuegoPorGeneroResponse>> ObtenerPorGeneroAsync(string genero)
    {
        var rows = await _session.ExecuteAsync(_selPorGenero.Bind(genero));
        return rows.Select(r => new VideojuegoPorGeneroResponse(r.GetValue<Guid>("videojuego_id"), r.GetValue<string>("nombre")));
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
}
