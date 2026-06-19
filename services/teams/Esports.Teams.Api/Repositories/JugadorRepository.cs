using Cassandra;
using Esports.Teams.Api.Cassandra;
using Esports.Teams.Api.Domain;
using Esports.Teams.Api.Dtos;

namespace Esports.Teams.Api.Repositories;

public interface IJugadorRepository
{
    Task<string> SiguienteCodigoJugadorAsync();
    Task AgregarJugadorAsync(Jugador jugador, Membresia membresiaActiva);
    Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname);
    Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais);
    Task<IEnumerable<JugadorResponse>> ObtenerPorEquipoAsync(Guid equipoId, string? pais);
    Task<IEnumerable<JugadorResponse>> ObtenerIntegrantesAsync(Guid equipoId);
    Task<IEnumerable<Guid>> ObtenerJugadorIdsAsync(Guid equipoId);
    Task<Jugador?> ObtenerPorIdAsync(Guid jugadorId);
    Task<JugadorResponse?> ObtenerPorCodigoAsync(string codigo);
    Task<IEnumerable<Membresia>> ObtenerMembresiasAsync(Guid jugadorId);
    Task LiberarAsync(Jugador jugador, Membresia membresiaActiva);
    Task FicharAsync(Jugador jugador, Membresia nuevaMembresia, string rol);
    Task TransferirAsync(Jugador jugador, Membresia membresiaActiva, Membresia nuevaMembresia, string rol);
    Task ActualizarContactoAsync(Jugador jugador, string nombre, string email, string telefono);
    Task EliminarAsync(Jugador jugador);
}

public class JugadorRepository : IJugadorRepository
{
    private readonly global::Cassandra.ISession _session;
    private readonly string _keyspace;

    // Alta
    private readonly PreparedStatement _insertJugador;
    private readonly PreparedStatement _insertPorNickname;
    private readonly PreparedStatement _insertPorPais;
    private readonly PreparedStatement _insertPorEquipo;
    private readonly PreparedStatement _insertIntegrante;
    private readonly PreparedStatement _insertPorCodigo;
    private readonly PreparedStatement _insertMembresia;

    // Lecturas
    private readonly PreparedStatement _selectPorNickname;
    private readonly PreparedStatement _selectPorPais;
    private readonly PreparedStatement _selectPorEquipo;
    private readonly PreparedStatement _selectPorEquipoPais;
    private readonly PreparedStatement _selectIntegrantes;
    private readonly PreparedStatement _selectById;
    private readonly PreparedStatement _selectByCodigo;
    private readonly PreparedStatement _selectMembresias;

    // Movimientos
    private readonly PreparedStatement _cerrarMembresia;
    private readonly PreparedStatement _deletePorEquipo;
    private readonly PreparedStatement _deleteIntegrante;
    private readonly PreparedStatement _moverJugadores;
    private readonly PreparedStatement _moverPorNickname;
    private readonly PreparedStatement _moverPorPais;
    private readonly PreparedStatement _moverPorCodigo;

    // RF-01: editar contacto / eliminar jugador
    private readonly PreparedStatement _editJugadores, _editPorNickname, _editPorCodigo, _editPorPais, _editPorEquipo, _editIntegrante;
    private readonly PreparedStatement _delJugador, _delPorNickname, _delPorPais, _delPorCodigo, _delMembresias;

    // Secuencia de código (LWT)
    private readonly PreparedStatement _selectSeq;
    private readonly PreparedStatement _insertSeqIfNotExists;
    private readonly PreparedStatement _updateSeqCas;

    public JugadorRepository(ICassandraSession cassandra, IConfiguration config)
    {
        _session = cassandra.Session;
        _keyspace = config["Cassandra:Keyspace"] ?? "esports_teams";

        _insertJugador = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores (jugador_id, codigo, nickname, nombre, pais, rol, email, telefono, equipo_id, fecha_registro) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
        _insertPorNickname = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores_por_nickname (nickname, jugador_id, codigo, nombre, pais, rol, email, telefono, equipo_id) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
        _insertPorPais = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores_por_pais (pais, jugador_id, codigo, nickname, nombre, rol, equipo_id) VALUES (?, ?, ?, ?, ?, ?, ?)");
        _insertPorEquipo = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugadores_por_equipo (equipo_id, pais, jugador_id, codigo, nickname, nombre, rol) VALUES (?, ?, ?, ?, ?, ?, ?)");
        _insertIntegrante = _session.Prepare(
            $"INSERT INTO {_keyspace}.integrantes_por_equipo (equipo_id, jugador_id, codigo, nickname, nombre, pais, rol) VALUES (?, ?, ?, ?, ?, ?, ?)");
        _insertPorCodigo = _session.Prepare(
            $"INSERT INTO {_keyspace}.jugador_por_codigo (codigo, jugador_id, nickname, nombre, pais, rol, email, telefono, equipo_id) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
        _insertMembresia = _session.Prepare(
            $"INSERT INTO {_keyspace}.membresias_por_jugador (jugador_id, fecha_desde, equipo_id, nombre_equipo, tag_equipo, rol, fecha_hasta) VALUES (?, ?, ?, ?, ?, ?, ?)");

        _selectPorNickname = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol, email, telefono, equipo_id FROM {_keyspace}.jugadores_por_nickname WHERE nickname = ?");
        _selectPorPais = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol, equipo_id FROM {_keyspace}.jugadores_por_pais WHERE pais = ?");
        _selectPorEquipo = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol FROM {_keyspace}.jugadores_por_equipo WHERE equipo_id = ?");
        _selectPorEquipoPais = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol FROM {_keyspace}.jugadores_por_equipo WHERE equipo_id = ? AND pais = ?");
        _selectIntegrantes = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol FROM {_keyspace}.integrantes_por_equipo WHERE equipo_id = ?");
        _selectById = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol, email, telefono, equipo_id, fecha_registro FROM {_keyspace}.jugadores WHERE jugador_id = ?");
        _selectByCodigo = _session.Prepare(
            $"SELECT jugador_id, codigo, nickname, nombre, pais, rol, email, telefono, equipo_id FROM {_keyspace}.jugador_por_codigo WHERE codigo = ?");
        _selectMembresias = _session.Prepare(
            $"SELECT jugador_id, fecha_desde, equipo_id, nombre_equipo, tag_equipo, rol, fecha_hasta FROM {_keyspace}.membresias_por_jugador WHERE jugador_id = ?");

        _cerrarMembresia = _session.Prepare(
            $"UPDATE {_keyspace}.membresias_por_jugador SET fecha_hasta = ? WHERE jugador_id = ? AND fecha_desde = ? AND equipo_id = ?");
        _deletePorEquipo = _session.Prepare(
            $"DELETE FROM {_keyspace}.jugadores_por_equipo WHERE equipo_id = ? AND pais = ? AND jugador_id = ?");
        _deleteIntegrante = _session.Prepare(
            $"DELETE FROM {_keyspace}.integrantes_por_equipo WHERE equipo_id = ? AND jugador_id = ?");
        _moverJugadores = _session.Prepare(
            $"UPDATE {_keyspace}.jugadores SET equipo_id = ?, rol = ? WHERE jugador_id = ?");
        _moverPorNickname = _session.Prepare(
            $"UPDATE {_keyspace}.jugadores_por_nickname SET equipo_id = ?, rol = ? WHERE nickname = ?");
        _moverPorPais = _session.Prepare(
            $"UPDATE {_keyspace}.jugadores_por_pais SET equipo_id = ?, rol = ? WHERE pais = ? AND jugador_id = ?");
        _moverPorCodigo = _session.Prepare(
            $"UPDATE {_keyspace}.jugador_por_codigo SET equipo_id = ?, rol = ? WHERE codigo = ?");

        _editJugadores = _session.Prepare($"UPDATE {_keyspace}.jugadores SET nombre = ?, email = ?, telefono = ? WHERE jugador_id = ?");
        _editPorNickname = _session.Prepare($"UPDATE {_keyspace}.jugadores_por_nickname SET nombre = ?, email = ?, telefono = ? WHERE nickname = ?");
        _editPorCodigo = _session.Prepare($"UPDATE {_keyspace}.jugador_por_codigo SET nombre = ?, email = ?, telefono = ? WHERE codigo = ?");
        _editPorPais = _session.Prepare($"UPDATE {_keyspace}.jugadores_por_pais SET nombre = ? WHERE pais = ? AND jugador_id = ?");
        _editPorEquipo = _session.Prepare($"UPDATE {_keyspace}.jugadores_por_equipo SET nombre = ? WHERE equipo_id = ? AND pais = ? AND jugador_id = ?");
        _editIntegrante = _session.Prepare($"UPDATE {_keyspace}.integrantes_por_equipo SET nombre = ? WHERE equipo_id = ? AND jugador_id = ?");

        _delJugador = _session.Prepare($"DELETE FROM {_keyspace}.jugadores WHERE jugador_id = ?");
        _delPorNickname = _session.Prepare($"DELETE FROM {_keyspace}.jugadores_por_nickname WHERE nickname = ?");
        _delPorPais = _session.Prepare($"DELETE FROM {_keyspace}.jugadores_por_pais WHERE pais = ? AND jugador_id = ?");
        _delPorCodigo = _session.Prepare($"DELETE FROM {_keyspace}.jugador_por_codigo WHERE codigo = ?");
        _delMembresias = _session.Prepare($"DELETE FROM {_keyspace}.membresias_por_jugador WHERE jugador_id = ?");

        _selectSeq = _session.Prepare(
            $"SELECT valor FROM {_keyspace}.secuencias WHERE nombre = ?");
        _insertSeqIfNotExists = _session.Prepare(
            $"INSERT INTO {_keyspace}.secuencias (nombre, valor) VALUES (?, ?) IF NOT EXISTS");
        _updateSeqCas = _session.Prepare(
            $"UPDATE {_keyspace}.secuencias SET valor = ? WHERE nombre = ? IF valor = ?");
    }

    // ─── Código legible J-001 vía LWT (compare-and-set) ──────────────────────────

    public async Task<string> SiguienteCodigoJugadorAsync()
    {
        const string clave = "jugador";
        for (var intento = 0; intento < 25; intento++)
        {
            var row = (await _session.ExecuteAsync(_selectSeq.Bind(clave))).FirstOrDefault();
            if (row is null)
            {
                var ins = (await _session.ExecuteAsync(_insertSeqIfNotExists.Bind(clave, 1))).First();
                if (ins.GetValue<bool>("[applied]")) return Formato(1);
                continue; // otro lo creó: releer y reintentar
            }

            var actual = row.GetValue<int>("valor");
            var siguiente = actual + 1;
            var cas = (await _session.ExecuteAsync(_updateSeqCas.Bind(siguiente, clave, actual))).First();
            if (cas.GetValue<bool>("[applied]")) return Formato(siguiente);
        }
        throw new InvalidOperationException("No se pudo generar el código de jugador (contención en la secuencia).");
    }

    private static string Formato(int n) => $"J-{n:D3}";

    // ─── Alta ────────────────────────────────────────────────────────────────────

    public async Task AgregarJugadorAsync(Jugador j, Membresia m)
    {
        // BATCH: 5 tablas de roster activo + jugador_por_codigo + membresía activa.
        var batch = new BatchStatement();
        batch.Add(_insertJugador.Bind(j.JugadorId, j.Codigo, j.Nickname, j.Nombre, j.Pais, j.Rol, j.Email, j.Telefono, j.EquipoId, j.FechaRegistro));
        batch.Add(_insertPorNickname.Bind(j.Nickname, j.JugadorId, j.Codigo, j.Nombre, j.Pais, j.Rol, j.Email, j.Telefono, j.EquipoId));
        batch.Add(_insertPorPais.Bind(j.Pais, j.JugadorId, j.Codigo, j.Nickname, j.Nombre, j.Rol, j.EquipoId));
        batch.Add(_insertPorEquipo.Bind(m.EquipoId, j.Pais, j.JugadorId, j.Codigo, j.Nickname, j.Nombre, j.Rol));
        batch.Add(_insertIntegrante.Bind(m.EquipoId, j.JugadorId, j.Codigo, j.Nickname, j.Nombre, j.Pais, j.Rol));
        batch.Add(_insertPorCodigo.Bind(j.Codigo, j.JugadorId, j.Nickname, j.Nombre, j.Pais, j.Rol, j.Email, j.Telefono, j.EquipoId));
        batch.Add(_insertMembresia.Bind(m.JugadorId, m.FechaDesde, m.EquipoId, m.NombreEquipo, m.TagEquipo, m.Rol, m.FechaHasta));
        await _session.ExecuteAsync(batch);
    }

    // ─── Lecturas ────────────────────────────────────────────────────────────────

    public async Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname)
    {
        var row = (await _session.ExecuteAsync(_selectPorNickname.Bind(nickname))).FirstOrDefault();
        return row is null ? null : MapConContacto(row);
    }

    public async Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais)
    {
        var rows = await _session.ExecuteAsync(_selectPorPais.Bind(pais));
        return rows.Select(MapSinContacto).ToList();
    }

    public async Task<IEnumerable<JugadorResponse>> ObtenerPorEquipoAsync(Guid equipoId, string? pais)
    {
        var rows = string.IsNullOrEmpty(pais)
            ? await _session.ExecuteAsync(_selectPorEquipo.Bind(equipoId))
            : await _session.ExecuteAsync(_selectPorEquipoPais.Bind(equipoId, pais));

        return rows.Select(r => MapRoster(r, equipoId)).ToList();
    }

    public async Task<IEnumerable<JugadorResponse>> ObtenerIntegrantesAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selectIntegrantes.Bind(equipoId));
        return rows.Select(r => MapRoster(r, equipoId)).ToList();
    }

    public async Task<IEnumerable<Guid>> ObtenerJugadorIdsAsync(Guid equipoId)
    {
        var rows = await _session.ExecuteAsync(_selectIntegrantes.Bind(equipoId));
        return rows.Select(r => r.GetValue<Guid>("jugador_id")).ToList();
    }

    public async Task<Jugador?> ObtenerPorIdAsync(Guid jugadorId)
    {
        var row = (await _session.ExecuteAsync(_selectById.Bind(jugadorId))).FirstOrDefault();
        if (row is null) return null;
        return new Jugador
        {
            JugadorId = row.GetValue<Guid>("jugador_id"),
            Codigo = row.GetValue<string>("codigo") ?? "",
            Nickname = row.GetValue<string>("nickname"),
            Nombre = row.GetValue<string>("nombre"),
            Pais = row.GetValue<string>("pais"),
            Rol = row.GetValue<string>("rol"),
            Email = row.GetValue<string>("email") ?? "",
            Telefono = row.GetValue<string>("telefono") ?? "",
            EquipoId = row.GetValue<Guid?>("equipo_id"),
            FechaRegistro = row.GetValue<DateTimeOffset>("fecha_registro")
        };
    }

    public async Task<JugadorResponse?> ObtenerPorCodigoAsync(string codigo)
    {
        var row = (await _session.ExecuteAsync(_selectByCodigo.Bind(codigo))).FirstOrDefault();
        return row is null ? null : MapConContacto(row);
    }

    public async Task<IEnumerable<Membresia>> ObtenerMembresiasAsync(Guid jugadorId)
    {
        var rows = await _session.ExecuteAsync(_selectMembresias.Bind(jugadorId));
        return rows.Select(r => new Membresia
        {
            JugadorId = r.GetValue<Guid>("jugador_id"),
            EquipoId = r.GetValue<Guid>("equipo_id"),
            NombreEquipo = r.GetValue<string>("nombre_equipo"),
            TagEquipo = r.GetValue<string>("tag_equipo"),
            Rol = r.GetValue<string>("rol"),
            FechaDesde = r.GetValue<DateTimeOffset>("fecha_desde"),
            FechaHasta = r.GetValue<DateTimeOffset?>("fecha_hasta")
        }).ToList();
    }

    // ─── Movimientos (baja / alta / traspaso), todos en BATCH atómico ────────────

    public async Task LiberarAsync(Jugador j, Membresia activa)
    {
        var batch = new BatchStatement();
        AgregarBajaDeEquipo(batch, j, activa);
        AgregarEquipoNull(batch, j);
        await _session.ExecuteAsync(batch);
    }

    public async Task FicharAsync(Jugador j, Membresia nueva, string rol)
    {
        var batch = new BatchStatement();
        AgregarAltaEnEquipo(batch, j, nueva, rol);
        await _session.ExecuteAsync(batch);
    }

    public async Task TransferirAsync(Jugador j, Membresia activa, Membresia nueva, string rol)
    {
        // Traspaso atómico (admin): baja del equipo actual + alta en el destino en un solo BATCH.
        // El equipo_id se escribe UNA sola vez (al destino, en la alta), nunca a null en el mismo
        // batch: dos escrituras al mismo cell con timestamp idéntico hacen ganar al tombstone (null)
        // y dejarían el cache del equipo activo vacío.
        var batch = new BatchStatement();
        AgregarBajaDeEquipo(batch, j, activa);
        AgregarAltaEnEquipo(batch, j, nueva, rol);
        await _session.ExecuteAsync(batch);
    }

    // ─── RF-01: editar contacto / eliminar jugador ───────────────────────────────

    public async Task ActualizarContactoAsync(Jugador j, string nombre, string email, string telefono)
    {
        var batch = new BatchStatement();
        // nombre + email + telefono donde se guardan
        batch.Add(_editJugadores.Bind(nombre, email, telefono, j.JugadorId));
        batch.Add(_editPorNickname.Bind(nombre, email, telefono, j.Nickname));
        batch.Add(_editPorCodigo.Bind(nombre, email, telefono, j.Codigo));
        // nombre desnormalizado en por-pais (siempre) y en el roster del equipo activo (si tiene)
        batch.Add(_editPorPais.Bind(nombre, j.Pais, j.JugadorId));
        if (j.EquipoId is Guid eq)
        {
            batch.Add(_editPorEquipo.Bind(nombre, eq, j.Pais, j.JugadorId));
            batch.Add(_editIntegrante.Bind(nombre, eq, j.JugadorId));
        }
        await _session.ExecuteAsync(batch);
    }

    public async Task EliminarAsync(Jugador j)
    {
        // Solo se elimina a un agente libre: no hay filas en jugadores_por_equipo/integrantes.
        var batch = new BatchStatement();
        batch.Add(_delJugador.Bind(j.JugadorId));
        batch.Add(_delPorNickname.Bind(j.Nickname));
        batch.Add(_delPorPais.Bind(j.Pais, j.JugadorId));
        batch.Add(_delPorCodigo.Bind(j.Codigo));
        batch.Add(_delMembresias.Bind(j.JugadorId));
        await _session.ExecuteAsync(batch);
    }

    private void AgregarBajaDeEquipo(BatchStatement batch, Jugador j, Membresia activa)
    {
        // Cierra la membresía (historial preservado) y saca del roster del equipo actual.
        batch.Add(_cerrarMembresia.Bind(DateTimeOffset.UtcNow, j.JugadorId, activa.FechaDesde, activa.EquipoId));
        batch.Add(_deletePorEquipo.Bind(activa.EquipoId, j.Pais, j.JugadorId));
        batch.Add(_deleteIntegrante.Bind(activa.EquipoId, j.JugadorId));
    }

    private void AgregarEquipoNull(BatchStatement batch, Jugador j)
    {
        // Solo para liberar (no para traspaso): deja al jugador como agente libre.
        batch.Add(_moverJugadores.Bind(null, j.Rol, j.JugadorId));
        batch.Add(_moverPorNickname.Bind(null, j.Rol, j.Nickname));
        batch.Add(_moverPorPais.Bind(null, j.Rol, j.Pais, j.JugadorId));
        batch.Add(_moverPorCodigo.Bind(null, j.Rol, j.Codigo));
    }

    private void AgregarAltaEnEquipo(BatchStatement batch, Jugador j, Membresia nueva, string rol)
    {
        // Inserta en el roster del destino, apunta equipo_id/rol al destino y abre membresía activa.
        batch.Add(_insertPorEquipo.Bind(nueva.EquipoId, j.Pais, j.JugadorId, j.Codigo, j.Nickname, j.Nombre, rol));
        batch.Add(_insertIntegrante.Bind(nueva.EquipoId, j.JugadorId, j.Codigo, j.Nickname, j.Nombre, j.Pais, rol));
        batch.Add(_moverJugadores.Bind(nueva.EquipoId, rol, j.JugadorId));
        batch.Add(_moverPorNickname.Bind(nueva.EquipoId, rol, j.Nickname));
        batch.Add(_moverPorPais.Bind(nueva.EquipoId, rol, j.Pais, j.JugadorId));
        batch.Add(_moverPorCodigo.Bind(nueva.EquipoId, rol, j.Codigo));
        batch.Add(_insertMembresia.Bind(nueva.JugadorId, nueva.FechaDesde, nueva.EquipoId, nueva.NombreEquipo, nueva.TagEquipo, rol, nueva.FechaHasta));
    }

    // ─── Mapeo ───────────────────────────────────────────────────────────────────

    // por-nickname y por-codigo guardan email/telefono.
    private static JugadorResponse MapConContacto(Row r) => new(
        r.GetValue<Guid>("jugador_id"),
        r.GetValue<string>("codigo") ?? "",
        r.GetValue<string>("nickname"),
        r.GetValue<string>("nombre"),
        r.GetValue<string>("pais"),
        r.GetValue<string>("rol"),
        r.GetValue<string>("email") ?? "",
        r.GetValue<string>("telefono") ?? "",
        r.GetValue<Guid?>("equipo_id"));

    // por-pais no guarda contacto (es índice de roster); email/telefono se ven en el detalle.
    private static JugadorResponse MapSinContacto(Row r) => new(
        r.GetValue<Guid>("jugador_id"),
        r.GetValue<string>("codigo") ?? "",
        r.GetValue<string>("nickname"),
        r.GetValue<string>("nombre"),
        r.GetValue<string>("pais"),
        r.GetValue<string>("rol"),
        "", "",
        r.GetValue<Guid?>("equipo_id"));

    private static JugadorResponse MapRoster(Row r, Guid equipoId) => new(
        r.GetValue<Guid>("jugador_id"),
        r.GetValue<string>("codigo") ?? "",
        r.GetValue<string>("nickname"),
        r.GetValue<string>("nombre"),
        r.GetValue<string>("pais"),
        r.GetValue<string>("rol"),
        "", "",
        equipoId);
}
