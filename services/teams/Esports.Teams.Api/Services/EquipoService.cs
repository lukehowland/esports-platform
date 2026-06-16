using Esports.Teams.Api.Domain;
using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Repositories;

namespace Esports.Teams.Api.Services;

public interface IEquipoService
{
    Task<EquipoResponse> CrearEquipoAsync(CrearEquipoRequest request);
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
            Nombre = request.Nombre,
            Tag = request.Tag,
            Pais = request.Pais,
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

    public async Task<JugadorResponse> AgregarJugadorAsync(Guid equipoId, AgregarJugadorRequest request)
    {
        var jugador = new Jugador
        {
            JugadorId = Guid.NewGuid(),
            Nickname = request.Nickname,
            Nombre = request.Nombre,
            Pais = request.Pais,
            Rol = request.Rol,
            EquipoId = equipoId,
            FechaRegistro = DateTimeOffset.UtcNow
        };
        await _jugadorRepo.AgregarJugadorAsync(jugador);
        return new JugadorResponse(jugador.JugadorId, jugador.Nickname, jugador.Nombre, jugador.Pais, jugador.Rol, jugador.EquipoId);
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
