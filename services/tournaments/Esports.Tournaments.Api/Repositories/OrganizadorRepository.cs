using Cassandra;
using Esports.Tournaments.Api.Cassandra;
using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;

namespace Esports.Tournaments.Api.Repositories;

public interface IOrganizadorRepository
{
    Task CrearAsync(Organizador o);
    Task<Organizador?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<OrganizadorResponse>> ObtenerTodosAsync();
    Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid organizadorId);
    Task<bool> TieneTorneosAsync(Guid organizadorId);
    Task ActualizarAsync(Guid id, string nombre, string email);
    Task EliminarAsync(Guid id);
}

public class OrganizadorRepository : IOrganizadorRepository
{
    private const string Bucket = "GLOBAL";

    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insBase, _insLista;
    private readonly PreparedStatement _selById, _selTodos, _selTorneos;
    private readonly PreparedStatement _selTorneosLimit;
    private readonly PreparedStatement _updBase, _updLista, _delBase, _delLista;

    public OrganizadorRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";
        _insBase = _session.Prepare($"INSERT INTO {_ks}.organizadores (organizador_id, nombre, email) VALUES (?, ?, ?)");
        _insLista = _session.Prepare($"INSERT INTO {_ks}.organizadores_lista (bucket, organizador_id, nombre, email) VALUES (?, ?, ?, ?)");
        _selById = _session.Prepare($"SELECT organizador_id, nombre, email FROM {_ks}.organizadores WHERE organizador_id = ?");
        _selTodos = _session.Prepare($"SELECT organizador_id, nombre, email FROM {_ks}.organizadores_lista WHERE bucket = ?");
        _selTorneos = _session.Prepare($"SELECT torneo_id, nombre_torneo, nombre_videojuego, fecha_inicio FROM {_ks}.torneos_por_organizador WHERE organizador_id = ?");
        _selTorneosLimit = _session.Prepare($"SELECT torneo_id FROM {_ks}.torneos_por_organizador WHERE organizador_id = ? LIMIT 1");
        _updBase = _session.Prepare($"UPDATE {_ks}.organizadores SET nombre = ?, email = ? WHERE organizador_id = ?");
        _updLista = _session.Prepare($"UPDATE {_ks}.organizadores_lista SET nombre = ?, email = ? WHERE bucket = ? AND organizador_id = ?");
        _delBase = _session.Prepare($"DELETE FROM {_ks}.organizadores WHERE organizador_id = ?");
        _delLista = _session.Prepare($"DELETE FROM {_ks}.organizadores_lista WHERE bucket = ? AND organizador_id = ?");
    }

    public async Task CrearAsync(Organizador o)
    {
        // BATCH: organizadores + organizadores_lista
        var batch = new BatchStatement();
        batch.Add(_insBase.Bind(o.OrganizadorId, o.Nombre, o.Email));
        batch.Add(_insLista.Bind(Bucket, o.OrganizadorId, o.Nombre, o.Email));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Organizador?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        return new Organizador
        {
            OrganizadorId = row.GetValue<Guid>("organizador_id"),
            Nombre = row.GetValue<string>("nombre"),
            Email = row.GetValue<string>("email") ?? ""
        };
    }

    public async Task<IEnumerable<OrganizadorResponse>> ObtenerTodosAsync()
    {
        var rows = await _session.ExecuteAsync(_selTodos.Bind("GLOBAL"));
        return rows.Select(r => new OrganizadorResponse(
            r.GetValue<Guid>("organizador_id"),
            r.GetValue<string>("nombre"),
            r.GetValue<string>("email") ?? ""));
    }

    public async Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid organizadorId)
    {
        var rows = await _session.ExecuteAsync(_selTorneos.Bind(organizadorId));
        return rows.Select(r => new TorneoResumenResponse(
            r.GetValue<Guid>("torneo_id"),
            r.GetValue<string>("nombre_torneo"),
            r.GetValue<string>("nombre_videojuego"),
            r.GetValue<DateTimeOffset>("fecha_inicio")));
    }

    public async Task<bool> TieneTorneosAsync(Guid organizadorId)
    {
        var rows = await _session.ExecuteAsync(_selTorneosLimit.Bind(organizadorId));
        return rows.Any();
    }

    public async Task ActualizarAsync(Guid id, string nombre, string email)
    {
        // El nombre vive desnormalizado en organizadores + organizadores_lista (mismo
        // servicio). Solo se permite renombrar cuando no hay torneos que copien el nombre,
        // así que un BATCH a estas dos tablas mantiene la consistencia.
        var batch = new BatchStatement();
        batch.Add(_updBase.Bind(nombre, email, id));
        batch.Add(_updLista.Bind(nombre, email, Bucket, id));
        await _session.ExecuteAsync(batch);
    }

    public async Task EliminarAsync(Guid id)
    {
        var batch = new BatchStatement();
        batch.Add(_delBase.Bind(id));
        batch.Add(_delLista.Bind(Bucket, id));
        await _session.ExecuteAsync(batch);
    }
}
