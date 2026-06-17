using Cassandra;
using Esports.Auth.Api.Cassandra;
using Esports.Auth.Api.Domain;

namespace Esports.Auth.Api.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _keyspace;
    private PreparedStatement? _getByUsername;
    private PreparedStatement? _insert;
    private PreparedStatement? _delete;

    public UsuarioRepository(ICassandraSession cassandraSession, IConfiguration config)
    {
        _session = cassandraSession.Session;
        _keyspace = config["Cassandra:Keyspace"] ?? "esports_auth";
    }

    private PreparedStatement GetByUsernamePrepared =>
        _getByUsername ??= _session.Prepare(
            $"SELECT username, password_hash, rol, organizador_id, equipo_id, nombre_display " +
            $"FROM {_keyspace}.usuarios WHERE username = ?");

    private PreparedStatement InsertPrepared =>
        _insert ??= _session.Prepare(
            $"INSERT INTO {_keyspace}.usuarios (username, password_hash, rol, organizador_id, equipo_id, nombre_display) " +
            $"VALUES (?, ?, ?, ?, ?, ?) IF NOT EXISTS");

    private PreparedStatement DeletePrepared =>
        _delete ??= _session.Prepare(
            $"DELETE FROM {_keyspace}.usuarios WHERE username = ? IF EXISTS");

    private static Usuario MapRow(Row row) => new()
    {
        Username = row.GetValue<string>("username"),
        PasswordHash = row.GetValue<string>("password_hash"),
        Rol = row.GetValue<string>("rol"),
        OrganizadorId = row.IsNull("organizador_id") ? null : row.GetValue<Guid>("organizador_id"),
        EquipoId = row.IsNull("equipo_id") ? null : row.GetValue<Guid>("equipo_id"),
        NombreDisplay = row.GetValue<string>("nombre_display"),
    };

    public async Task<Usuario?> GetByUsernameAsync(string username)
    {
        var rs = await _session.ExecuteAsync(GetByUsernamePrepared.Bind(username));
        var row = rs.FirstOrDefault();
        return row is null ? null : MapRow(row);
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        // Sin WHERE: escaneo completo de la tabla. Es deliberado y acotado: endpoint solo-admin
        // sobre una tabla de usuarios demo pequeña. No requiere ALLOW FILTERING.
        var rs = await _session.ExecuteAsync(new SimpleStatement(
            $"SELECT username, password_hash, rol, organizador_id, equipo_id, nombre_display FROM {_keyspace}.usuarios"));
        return rs.Select(MapRow).ToList();
    }

    public async Task<bool> DeleteAsync(string username)
    {
        var rs = await _session.ExecuteAsync(DeletePrepared.Bind(username));
        return rs.FirstOrDefault()?.GetValue<bool>("[applied]") ?? false;
    }

    public async Task<bool> CreateAsync(Usuario usuario)
    {
        var rs = await _session.ExecuteAsync(InsertPrepared.Bind(
            usuario.Username,
            usuario.PasswordHash,
            usuario.Rol,
            usuario.OrganizadorId.HasValue ? (object)usuario.OrganizadorId.Value : null,
            usuario.EquipoId.HasValue ? (object)usuario.EquipoId.Value : null,
            usuario.NombreDisplay));

        var row = rs.FirstOrDefault();
        return row?.GetValue<bool>("[applied]") ?? false;
    }
}
