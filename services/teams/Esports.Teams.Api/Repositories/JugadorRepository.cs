using Cassandra;
using Esports.Teams.Api.Cassandra;
using Esports.Teams.Api.Domain;
using Esports.Teams.Api.Dtos;

namespace Esports.Teams.Api.Repositories;

public interface IJugadorRepository
{
    Task AgregarJugadorAsync(Jugador jugador);
    Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname);
    Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais);
    Task<IEnumerable<JugadorResponse>> ObtenerPorEquipoAsync(Guid equipoId, string? pais);
    Task<IEnumerable<JugadorResponse>> ObtenerIntegrantesAsync(Guid equipoId);
    Task<IEnumerable<Guid>> ObtenerJugadorIdsAsync(Guid equipoId);
}

public class JugadorRepository : IJugadorRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _keyspace;

    private readonly PreparedStatement _insertJugador;
    private readonly PreparedStatement _insertPorNickname;
    private readonly PreparedStatement _insertPorPais;
    private readonly PreparedStatement _insertPorEquipo;
    private readonly PreparedStatement _insertIntegrante;

    private readonly PreparedStatement _selectPorNickname;
    private readonly PreparedStatement _selectPorPais;
    private readonly PreparedStatement _selectPorEquipo;
    private readonly PreparedStatement _selectPorEquipoPais;
    private readonly PreparedStatement _selectIntegrantes;

    public JugadorRepository(ICassandraSession cassandra, IConfiguration config)
    {
        _session = cassandra.Session;
        _keyspace = config["Cassandra:Keyspace"] ?? "esports_teams";

        _insertJugador = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores (jugador_id, nickname, nombre, pais, rol, equipo_id, fecha_registro) VALUES (?, ?, ?, ?, ?, ?, ?)");
        _insertPorNickname = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores_por_nickname (nickname, jugador_id, nombre, pais, rol, equipo_id) VALUES (?, ?, ?, ?, ?, ?)");
        _insertPorPais = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores_por_pais (pais, jugador_id, nickname, nombre, rol, equipo_id) VALUES (?, ?, ?, ?, ?, ?)");
        _insertPorEquipo = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores_por_equipo (equipo_id, pais, jugador_id, nickname, nombre, rol) VALUES (?, ?, ?, ?, ?, ?)");
        _insertIntegrante = _session.Prepare(
            $"INSERT INTO {_keyspace}.integrantes_por_equipo (equipo_id, jugador_id, nickname, nombre, pais, rol) VALUES (?, ?, ?, ?, ?, ?)");

        _selectPorNickname = _session.Prepare(
            $"SELECT jugador_id, nickname, nombre, pais, rol, equipo_id FROM {_keyspace}.jugadores_por_nickname WHERE nickname = ?");
        _selectPorPais = _session.Prepare(
            $"SELECT jugador_id, nickname, nombre, pais, rol, equipo_id FROM {_keyspace}.jugadores_por_pais WHERE pais = ?");
        // Q3 sin filtro de país — prefix scan válido (no ALLOW FILTERING)
        _selectPorEquipo = _session.Prepare(
            $"SELECT jugador_id, nickname, nombre, pais, rol FROM {_keyspace}.jugadores_por_equipo WHERE equipo_id = ?");
        // Q3 con filtro de país
        _selectPorEquipoPais = _session.Prepare(
            $"SELECT jugador_id, nickname, nombre, pais, rol FROM {_keyspace}.jugadores_por_equipo WHERE equipo_id = ? AND pais = ?");
        _selectIntegrantes = _session.Prepare(
            $"SELECT jugador_id, nickname, nombre, pais, rol FROM {_keyspace}.integrantes_por_equipo WHERE equipo_id = ?");
    }

    public async Task AgregarJugadorAsync(Jugador j)
    {
        // BATCH: jugadores + jugadores_por_nickname + jugadores_por_pais + jugadores_por_equipo + integrantes_por_equipo
        var batch = new BatchStatement();
        batch.Add(_insertJugador.Bind(j.JugadorId, j.Nickname, j.Nombre, j.Pais, j.Rol, j.EquipoId, j.FechaRegistro));
        batch.Add(_insertPorNickname.Bind(j.Nickname, j.JugadorId, j.Nombre, j.Pais, j.Rol, j.EquipoId));
        batch.Add(_insertPorPais.Bind(j.Pais, j.JugadorId, j.Nickname, j.Nombre, j.Rol, j.EquipoId));
        batch.Add(_insertPorEquipo.Bind(j.EquipoId, j.Pais, j.JugadorId, j.Nickname, j.Nombre, j.Rol));
        batch.Add(_insertIntegrante.Bind(j.EquipoId, j.JugadorId, j.Nickname, j.Nombre, j.Pais, j.Rol));
        await _session.ExecuteAsync(batch);
    }

    public async Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname)
    {
        var row = (await _session.ExecuteAsync(_selectPorNickname.Bind(nickname))).FirstOrDefault();
        if (row is null) return null;
        return MapJugador(row);
    }

    public async Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais)
    {
        var rows = await _session.ExecuteAsync(_selectPorPais.Bind(pais));
        return rows.Select(MapJugador);
    }

    public async Task<IEnumerable<JugadorResponse>> ObtenerPorEquipoAsync(Guid equipoId, string? pais)
    {
        RowSet rows;
        if (string.IsNullOrEmpty(pais))
            rows = await _session.ExecuteAsync(_selectPorEquipo.Bind(equipoId));
        else
            rows = await _session.ExecuteAsync(_selectPorEquipoPais.Bind(equipoId, pais));

        return rows.Select(r => new JugadorResponse(
            r.GetValue<Guid>("jugador_id"),
            r.GetValue<string>("nickname"),
            r.GetValue<string>("nombre"),
            r.GetValue<string>("pais"),
            r.GetValue<string>("rol"),
            equipoId));
    }

    public async Task<IEnumerable<JugadorResponse>> ObtenerIntegrantesAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selectIntegrantes.Bind(equipoId));
        return rows.Select(r => new JugadorResponse(
            r.GetValue<Guid>("jugador_id"),
            r.GetValue<string>("nickname"),
            r.GetValue<string>("nombre"),
            r.GetValue<string>("pais"),
            r.GetValue<string>("rol"),
            equipoId));
    }

    public async Task<IEnumerable<Guid>> ObtenerJugadorIdsAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selectIntegrantes.Bind(equipoId));
        return rows.Select(r => r.GetValue<Guid>("jugador_id")).ToList();
    }

    private static JugadorResponse MapJugador(Row r) => new(
        r.GetValue<Guid>("jugador_id"),
        r.GetValue<string>("nickname"),
        r.GetValue<string>("nombre"),
        r.GetValue<string>("pais"),
        r.GetValue<string>("rol"),
        r.GetValue<Guid>("equipo_id"));
}
