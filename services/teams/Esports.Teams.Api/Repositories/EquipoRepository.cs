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
    Task<bool> TieneIntegrantesAsync(Guid equipoId);
    Task ActualizarAsync(Equipo original, Equipo nuevo);
    Task EliminarAsync(Equipo equipo);
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
    private readonly PreparedStatement _tieneIntegrantes;
    private readonly PreparedStatement _updEquipo, _updEquipoPorFecha, _updEquipoPorTag;
    private readonly PreparedStatement _delEquipo, _delEquipoPorFecha, _delEquipoPorTag;

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

        // RF-02: editar / eliminar (block-on-dependents = tiene integrantes)
        _tieneIntegrantes = _session.Prepare(
            $"SELECT jugador_id FROM {_keyspace}.integrantes_por_equipo WHERE equipo_id = ? LIMIT 1");
        _updEquipo = _session.Prepare(
            $"UPDATE {_keyspace}.equipos SET nombre = ?, tag = ?, pais = ? WHERE equipo_id = ?");
        _updEquipoPorFecha = _session.Prepare(
            $"UPDATE {_keyspace}.equipos_por_fecha SET nombre = ?, tag = ?, pais = ? WHERE bucket = ? AND fecha_creacion = ? AND equipo_id = ?");
        _updEquipoPorTag = _session.Prepare(
            $"UPDATE {_keyspace}.equipos_por_tag SET nombre = ?, pais = ? WHERE tag = ?");
        _delEquipo = _session.Prepare(
            $"DELETE FROM {_keyspace}.equipos WHERE equipo_id = ?");
        _delEquipoPorFecha = _session.Prepare(
            $"DELETE FROM {_keyspace}.equipos_por_fecha WHERE bucket = ? AND fecha_creacion = ? AND equipo_id = ?");
        _delEquipoPorTag = _session.Prepare(
            $"DELETE FROM {_keyspace}.equipos_por_tag WHERE tag = ?");
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

    public async Task<bool> TieneIntegrantesAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_tieneIntegrantes.Bind(equipoId));
        return rows.Any();
    }

    public async Task ActualizarAsync(Equipo original, Equipo nuevo)
    {
        // BATCH: equipos + equipos_por_fecha (nombre/tag/pais son columnas de valor).
        // El tag es la PK de equipos_por_tag: si cambia, se borra la fila vieja y se inserta
        // la nueva; si no, se actualiza nombre/pais. Solo se edita sin integrantes, así que
        // no hay copias del nombre del equipo en otros servicios que queden obsoletas.
        var batch = new BatchStatement();
        batch.Add(_updEquipo.Bind(nuevo.Nombre, nuevo.Tag, nuevo.Pais, original.EquipoId));
        batch.Add(_updEquipoPorFecha.Bind(nuevo.Nombre, nuevo.Tag, nuevo.Pais, "GLOBAL", original.FechaCreacion, original.EquipoId));

        if (string.Equals(nuevo.Tag, original.Tag, StringComparison.Ordinal))
        {
            batch.Add(_updEquipoPorTag.Bind(nuevo.Nombre, nuevo.Pais, nuevo.Tag));
        }
        else
        {
            batch.Add(_delEquipoPorTag.Bind(original.Tag));
            batch.Add(_insertEquipoPorTag.Bind(nuevo.Tag, original.EquipoId, nuevo.Nombre, nuevo.Pais));
        }

        await _session.ExecuteAsync(batch);
    }

    public async Task EliminarAsync(Equipo equipo)
    {
        var batch = new BatchStatement();
        batch.Add(_delEquipo.Bind(equipo.EquipoId));
        batch.Add(_delEquipoPorFecha.Bind("GLOBAL", equipo.FechaCreacion, equipo.EquipoId));
        batch.Add(_delEquipoPorTag.Bind(equipo.Tag));
        await _session.ExecuteAsync(batch);
    }
}
