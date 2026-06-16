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
}

public class OrganizadorRepository : IOrganizadorRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _ks;
    private readonly PreparedStatement _insBase, _insLista;
    private readonly PreparedStatement _selById, _selTodos, _selTorneos;

    public OrganizadorRepository(ICassandraSession c, IConfiguration config)
    {
        _session = c.Session;
        _ks = config["Cassandra:Keyspace"] ?? "esports_tournaments";
        _insBase = _session.Prepare($"INSERT INTO {_ks}.organizadores (organizador_id, nombre) VALUES (?, ?)");
        _insLista = _session.Prepare($"INSERT INTO {_ks}.organizadores_lista (bucket, organizador_id, nombre) VALUES (?, ?, ?)");
        _selById = _session.Prepare($"SELECT organizador_id, nombre FROM {_ks}.organizadores WHERE organizador_id = ?");
        _selTodos = _session.Prepare($"SELECT organizador_id, nombre FROM {_ks}.organizadores_lista WHERE bucket = ?");
        _selTorneos = _session.Prepare($"SELECT torneo_id, nombre_torneo, nombre_videojuego, fecha_inicio FROM {_ks}.torneos_por_organizador WHERE organizador_id = ?");
    }

    public async Task CrearAsync(Organizador o)
    {
        // BATCH: organizadores + organizadores_lista
        var batch = new BatchStatement();
        batch.Add(_insBase.Bind(o.OrganizadorId, o.Nombre));
        batch.Add(_insLista.Bind("GLOBAL", o.OrganizadorId, o.Nombre));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Organizador?> ObtenerPorIdAsync(Guid id)
    {
        var row = (await _session.ExecuteAsync(_selById.Bind(id))).FirstOrDefault();
        if (row is null) return null;
        return new Organizador { OrganizadorId = row.GetValue<Guid>("organizador_id"), Nombre = row.GetValue<string>("nombre") };
    }

    public async Task<IEnumerable<OrganizadorResponse>> ObtenerTodosAsync()
    {
        var rows = await _session.ExecuteAsync(_selTodos.Bind("GLOBAL"));
        return rows.Select(r => new OrganizadorResponse(r.GetValue<Guid>("organizador_id"), r.GetValue<string>("nombre")));
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
}
