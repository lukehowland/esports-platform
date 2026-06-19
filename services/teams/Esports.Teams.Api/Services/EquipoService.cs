using Esports.Teams.Api.Domain;
using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Repositories;

namespace Esports.Teams.Api.Services;

public interface IEquipoService
{
    Task<EquipoResponse> CrearEquipoAsync(CrearEquipoRequest request);
    Task<MutacionResultado> ActualizarEquipoAsync(Guid equipoId, EditarEquipoRequest request);
    Task<MutacionResultado> EliminarEquipoAsync(Guid equipoId);
    Task<EquipoResponse?> ObtenerPorIdAsync(Guid equipoId);
    Task<JugadorResponse> AgregarJugadorAsync(Guid equipoId, AgregarJugadorRequest request);
    Task<IEnumerable<EquipoResponse>> ObtenerPorFechaAsync();
    Task<EquipoResponse?> ObtenerPorTagAsync(string tag);
    Task<IEnumerable<JugadorResponse>> ObtenerJugadoresPorEquipoAsync(Guid equipoId, string? pais);
    Task<IEnumerable<JugadorResponse>> ObtenerIntegrantesAsync(Guid equipoId);
    Task<IEnumerable<Guid>> ObtenerJugadorIdsAsync(Guid equipoId);
}

public class EquipoService : IEquipoService
{
    private readonly IEquipoRepository _equipoRepo;
    private readonly IJugadorRepository _jugadorRepo;

    public EquipoService(IEquipoRepository equipoRepo, IJugadorRepository jugadorRepo)
    {
        _equipoRepo = equipoRepo;
        _jugadorRepo = jugadorRepo;
    }

    public async Task<EquipoResponse> CrearEquipoAsync(CrearEquipoRequest request)
    {
        var equipo = new Equipo
        {
            EquipoId = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Tag = request.Tag.Trim().ToUpperInvariant(),
            Pais = request.Pais.Trim().ToUpperInvariant(),
            FechaCreacion = DateTimeOffset.UtcNow
        };
        await _equipoRepo.CrearEquipoAsync(equipo);
        return new EquipoResponse(equipo.EquipoId, equipo.Nombre, equipo.Tag, equipo.Pais, equipo.FechaCreacion);
    }

    public async Task<EquipoResponse?> ObtenerPorIdAsync(Guid equipoId)
    {
        var equipo = await _equipoRepo.ObtenerPorIdAsync(equipoId);
        if (equipo is null) return null;
        return new EquipoResponse(equipo.EquipoId, equipo.Nombre, equipo.Tag, equipo.Pais, equipo.FechaCreacion);
    }

    public async Task<MutacionResultado> ActualizarEquipoAsync(Guid equipoId, EditarEquipoRequest request)
    {
        var original = await _equipoRepo.ObtenerPorIdAsync(equipoId);
        if (original is null) return MutacionResultado.NoEncontrado;

        // Block-on-dependents: el nombre/tag del equipo está desnormalizado en otros servicios
        // (inscripciones, partidas, premios, membresías). Solo se edita sin roster.
        if (await _equipoRepo.TieneIntegrantesAsync(equipoId))
            return MutacionResultado.ConDependencias;

        var nuevo = new Equipo
        {
            EquipoId = equipoId,
            Nombre = request.Nombre.Trim(),
            Tag = request.Tag.Trim().ToUpperInvariant(),
            Pais = request.Pais.Trim().ToUpperInvariant(),
            FechaCreacion = original.FechaCreacion
        };
        await _equipoRepo.ActualizarAsync(original, nuevo);
        return MutacionResultado.Ok;
    }

    public async Task<MutacionResultado> EliminarEquipoAsync(Guid equipoId)
    {
        var equipo = await _equipoRepo.ObtenerPorIdAsync(equipoId);
        if (equipo is null) return MutacionResultado.NoEncontrado;

        if (await _equipoRepo.TieneIntegrantesAsync(equipoId))
            return MutacionResultado.ConDependencias;

        await _equipoRepo.EliminarAsync(equipo);
        return MutacionResultado.Ok;
    }

    public async Task<JugadorResponse> AgregarJugadorAsync(Guid equipoId, AgregarJugadorRequest request)
    {
        var equipo = await _equipoRepo.ObtenerPorIdAsync(equipoId)
            ?? throw new InvalidOperationException($"El equipo {equipoId} no existe.");

        var ahora = DateTimeOffset.UtcNow;
        var jugador = new Jugador
        {
            JugadorId = Guid.NewGuid(),
            Codigo = await _jugadorRepo.SiguienteCodigoJugadorAsync(),
            Nickname = request.Nickname.Trim(),
            Nombre = request.Nombre.Trim(),
            Pais = request.Pais.Trim().ToUpperInvariant(),
            Rol = request.Rol.Trim().ToUpperInvariant(),
            Email = request.Email.Trim(),
            Telefono = request.Telefono.Trim(),
            EquipoId = equipoId,
            FechaRegistro = ahora
        };

        // RF-03: el alta abre la membresía activa del jugador en su primer equipo.
        var membresia = new Membresia
        {
            JugadorId = jugador.JugadorId,
            EquipoId = equipoId,
            NombreEquipo = equipo.Nombre,
            TagEquipo = equipo.Tag,
            Rol = jugador.Rol,
            FechaDesde = ahora,
            FechaHasta = null
        };

        await _jugadorRepo.AgregarJugadorAsync(jugador, membresia);
        return new JugadorResponse(jugador.JugadorId, jugador.Codigo, jugador.Nickname, jugador.Nombre, jugador.Pais, jugador.Rol, jugador.Email, jugador.Telefono, jugador.EquipoId);
    }

    public Task<IEnumerable<EquipoResponse>> ObtenerPorFechaAsync() => _equipoRepo.ObtenerPorFechaAsync();

    public Task<EquipoResponse?> ObtenerPorTagAsync(string tag) => _equipoRepo.ObtenerPorTagAsync(tag);

    public Task<IEnumerable<JugadorResponse>> ObtenerJugadoresPorEquipoAsync(Guid equipoId, string? pais) =>
        _jugadorRepo.ObtenerPorEquipoAsync(equipoId, pais);

    public Task<IEnumerable<JugadorResponse>> ObtenerIntegrantesAsync(Guid equipoId) =>
        _jugadorRepo.ObtenerIntegrantesAsync(equipoId);

    public Task<IEnumerable<Guid>> ObtenerJugadorIdsAsync(Guid equipoId) =>
        _jugadorRepo.ObtenerJugadorIdsAsync(equipoId);
}
