using Esports.Teams.Api.Domain;
using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Repositories;

namespace Esports.Teams.Api.Services;

public interface IJugadorService
{
    Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname);
    Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais);
    Task<JugadorResponse?> ObtenerPorIdAsync(Guid jugadorId);
    Task<JugadorResponse?> ObtenerPorCodigoAsync(string codigo);
    Task<IEnumerable<MembresiaResponse>?> ObtenerMembresiasAsync(Guid jugadorId);
    Task<LiberacionResultado> LiberarAsync(Guid jugadorId);
    Task<AsignacionResultado> AsignarAsync(Guid jugadorId, Guid equipoDestinoId, string? rol, bool esAdmin);
}

public class JugadorService : IJugadorService
{
    private readonly IJugadorRepository _repo;
    private readonly IEquipoRepository _equipoRepo;

    public JugadorService(IJugadorRepository repo, IEquipoRepository equipoRepo)
    {
        _repo = repo;
        _equipoRepo = equipoRepo;
    }

    public Task<JugadorResponse?> ObtenerPorNicknameAsync(string nickname) =>
        _repo.ObtenerPorNicknameAsync(nickname);

    public Task<IEnumerable<JugadorResponse>> ObtenerPorPaisAsync(string pais) =>
        _repo.ObtenerPorPaisAsync(pais);

    public async Task<JugadorResponse?> ObtenerPorIdAsync(Guid jugadorId)
    {
        var j = await _repo.ObtenerPorIdAsync(jugadorId);
        return j is null ? null : Map(j);
    }

    public Task<JugadorResponse?> ObtenerPorCodigoAsync(string codigo) =>
        _repo.ObtenerPorCodigoAsync(codigo);

    public async Task<IEnumerable<MembresiaResponse>?> ObtenerMembresiasAsync(Guid jugadorId)
    {
        var jugador = await _repo.ObtenerPorIdAsync(jugadorId);
        if (jugador is null) return null;

        var membresias = await _repo.ObtenerMembresiasAsync(jugadorId);
        return membresias
            .OrderByDescending(m => m.FechaDesde)
            .Select(m => new MembresiaResponse(
                m.EquipoId, m.NombreEquipo, m.TagEquipo, m.Rol, m.FechaDesde, m.FechaHasta, m.Activa))
            .ToList();
    }

    public async Task<LiberacionResultado> LiberarAsync(Guid jugadorId)
    {
        var jugador = await _repo.ObtenerPorIdAsync(jugadorId);
        if (jugador is null) return LiberacionResultado.JugadorNoEncontrado;

        var activa = (await _repo.ObtenerMembresiasAsync(jugadorId)).FirstOrDefault(m => m.Activa);
        if (activa is null) return LiberacionResultado.YaEsAgenteLibre;

        await _repo.LiberarAsync(jugador, activa);
        return LiberacionResultado.Ok;
    }

    public async Task<AsignacionResultado> AsignarAsync(Guid jugadorId, Guid equipoDestinoId, string? rol, bool esAdmin)
    {
        var jugador = await _repo.ObtenerPorIdAsync(jugadorId);
        if (jugador is null) return AsignacionResultado.JugadorNoEncontrado;

        var destino = await _equipoRepo.ObtenerPorIdAsync(equipoDestinoId);
        if (destino is null) return AsignacionResultado.EquipoNoEncontrado;

        var activa = (await _repo.ObtenerMembresiasAsync(jugadorId)).FirstOrDefault(m => m.Activa);
        var rolFinal = string.IsNullOrWhiteSpace(rol) ? jugador.Rol : rol.Trim().ToUpperInvariant();
        var nueva = new Membresia
        {
            JugadorId = jugadorId,
            EquipoId = destino.EquipoId,
            NombreEquipo = destino.Nombre,
            TagEquipo = destino.Tag,
            Rol = rolFinal,
            FechaDesde = DateTimeOffset.UtcNow,
            FechaHasta = null
        };

        if (activa is null)
        {
            // Agente libre → fichar (admin o capitán del destino).
            await _repo.FicharAsync(jugador, nueva, rolFinal);
            return AsignacionResultado.Ok;
        }

        if (activa.EquipoId == equipoDestinoId)
            return AsignacionResultado.YaEnEseEquipo;

        // Tiene equipo activo en otro lado: solo el admin transfiere atómicamente.
        if (!esAdmin) return AsignacionResultado.RequiereLiberar;

        await _repo.TransferirAsync(jugador, activa, nueva, rolFinal);
        return AsignacionResultado.Ok;
    }

    private static JugadorResponse Map(Jugador j) =>
        new(j.JugadorId, j.Codigo, j.Nickname, j.Nombre, j.Pais, j.Rol, j.EquipoId);
}
