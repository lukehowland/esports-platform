using Cassandra;
using Esports.Teams.Api.Cassandra;
using Esports.Teams.Api.Domain;
using Esports.Teams.Api.Dtos;

namespace Esports.Teams.Api.Repositories;

public interface IEquipoRepository
{
    Task CrearEquipoAsync(Equipo equipo);
    Task<Equipo?> ObtenerPorIdAsync(Guid equipoId);
    Task<IEnumerable<EquipoResponse>> ObtenerPorFechaAsync();
    Task<EquipoResponse?> ObtenerPorTagAsync(string tag);
}

public class EquipoRepository : IEquipoRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _keyspace;

    private readonly PreparedStatement _insertEquipo;
    private readonly PreparedStatement _insertEquipoPorFecha;
    private readonly PreparedStatement _insertEquipoPorTag;
    private readonly PreparedStatement _selectById;
    private readonly PreparedStatement _selectPorFecha;
    private readonly PreparedStatement _selectPorTag;

    public EquipoRepository(ICassandraSession cassandra, IConfiguration config)
    {
        _session = cassandra.Session;
        _keyspace = config["Cassandra:Keyspace"] ?? "esports_teams";

        _insertEquipo = _session.Prepare(
            $"INSERT INTO {_keyspace}.equipos (equipo_id, nombre, tag, pais, fecha_creacion) VALUES (?, ?, ?, ?, ?)");
        _insertEquipoPorFecha = _session.Prepare(
            $"INSERT INTO {_keyspace}.equipos_por_fecha (bucket, fecha_creacion, equipo_id, nombre, tag, pais) VALUES (?, ?, ?, ?, ?, ?)");
        _insertEquipoPorTag = _session.Prepare(
            $"INSERT INTO {_keyspace}.equipos_por_tag (tag, equipo_id, nombre, pais) VALUES (?, ?, ?, ?)");
        _selectById = _session.Prepare(
            $"SELECT equipo_id, nombre, tag, pais, fecha_creacion FROM {_keyspace}.equipos WHERE equipo_id = ?");
        _selectPorFecha = _session.Prepare(
            $"SELECT equipo_id, nombre, tag, pais, fecha_creacion FROM {_keyspace}.equipos_por_fecha WHERE bucket = ?");
        _selectPorTag = _session.Prepare(
            $"SELECT equipo_id, nombre, pais FROM {_keyspace}.equipos_por_tag WHERE tag = ?");
    }

    public async Task CrearEquipoAsync(Equipo equipo)
    {
        // BATCH: equipos + equipos_por_fecha + equipos_por_tag
        var batch = new BatchStatement();
        batch.Add(_insertEquipo.Bind(equipo.EquipoId, equipo.Nombre, equipo.Tag, equipo.Pais, equipo.FechaCreacion));
        batch.Add(_insertEquipoPorFecha.Bind("GLOBAL", equipo.FechaCreacion, equipo.EquipoId, equipo.Nombre, equipo.Tag, equipo.Pais));
        batch.Add(_insertEquipoPorTag.Bind(equipo.Tag, equipo.EquipoId, equipo.Nombre, equipo.Pais));
        await _session.ExecuteAsync(batch);
    }

    public async Task<Equipo?> ObtenerPorIdAsync(Guid equipoId)
    {
        var row = (await _session.ExecuteAsync(_selectById.Bind(equipoId))).FirstOrDefault();
        if (row is null) return null;
        return new Equipo
        {
            EquipoId = row.GetValue<Guid>("equipo_id"),
            Nombre = row.GetValue<string>("nombre"),
            Tag = row.GetValue<string>("tag"),
            Pais = row.GetValue<string>("pais"),
            FechaCreacion = row.GetValue<DateTimeOffset>("fecha_creacion")
        };
    }

    public async Task<IEnumerable<EquipoResponse>> ObtenerPorFechaAsync()
    {
        var rows = await _session.ExecuteAsync(_selectPorFecha.Bind("GLOBAL"));
        return rows.Select(r => new EquipoResponse(
            r.GetValue<Guid>("equipo_id"),
            r.GetValue<string>("nombre"),
            r.GetValue<string>("tag"),
            r.GetValue<string>("pais"),
            r.GetValue<DateTimeOffset>("fecha_creacion")));
    }

    public async Task<EquipoResponse?> ObtenerPorTagAsync(string tag)
    {
        var row = (await _session.ExecuteAsync(_selectPorTag.Bind(tag))).FirstOrDefault();
        if (row is null) return null;
        return new EquipoResponse(
            row.GetValue<Guid>("equipo_id"),
            row.GetValue<string>("nombre"),
            tag,
            row.GetValue<string>("pais"),
            default);
    }
}
