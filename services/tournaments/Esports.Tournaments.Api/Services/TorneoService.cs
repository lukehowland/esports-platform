using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Repositories;

namespace Esports.Tournaments.Api.Services;

public interface ITorneoService
{
    Task<TorneoResponse> CrearAsync(CrearTorneoRequest req);
    Task<TorneoResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync();
    Task<TorneoPorCodigoResponse?> ObtenerPorCodigoAsync(string codigo);
    Task<(MutacionResultado Resultado, TorneoResponse? Torneo)> ActualizarAsync(Guid id, EditarTorneoRequest req);
    Task<MutacionResultado> EliminarAsync(Guid id);
}

public class TorneoService : ITorneoService
{
    private readonly ITorneoRepository _torneoRepo;
    private readonly IVideojuegoRepository _videojuegoRepo;
    private readonly IOrganizadorRepository _organizadorRepo;

    public TorneoService(ITorneoRepository torneoRepo, IVideojuegoRepository videojuegoRepo,
        IOrganizadorRepository organizadorRepo)
    {
        _torneoRepo = torneoRepo;
        _videojuegoRepo = videojuegoRepo;
        _organizadorRepo = organizadorRepo;
    }

    public async Task<TorneoResponse> CrearAsync(CrearTorneoRequest req)
    {
        var videojuego = await _videojuegoRepo.ObtenerPorIdAsync(req.VideojuegoId)
            ?? throw new KeyNotFoundException($"Videojuego {req.VideojuegoId} no encontrado.");
        var organizador = await _organizadorRepo.ObtenerPorIdAsync(req.OrganizadorId)
            ?? throw new KeyNotFoundException($"Organizador {req.OrganizadorId} no encontrado.");

        var t = new Torneo
        {
            TorneoId = Guid.NewGuid(),
            Nombre = req.Nombre.Trim(),
            Codigo = req.Codigo.Trim().ToUpperInvariant(),
            VideojuegoId = videojuego.VideojuegoId,
            NombreVideojuego = videojuego.Nombre,
            OrganizadorId = organizador.OrganizadorId,
            NombreOrganizador = organizador.Nombre,
            FechaInicio = req.FechaInicio,
            FechaFin = req.FechaFin
        };
        await _torneoRepo.CrearAsync(t);
        return new TorneoResponse(t.TorneoId, t.Nombre, t.Codigo, t.VideojuegoId, t.NombreVideojuego,
            t.OrganizadorId, t.NombreOrganizador, t.FechaInicio, t.FechaFin);
    }

    public async Task<TorneoResponse?> ObtenerPorIdAsync(Guid id)
    {
        var t = await _torneoRepo.ObtenerPorIdAsync(id);
        return t is null ? null : new TorneoResponse(t.TorneoId, t.Nombre, t.Codigo, t.VideojuegoId,
            t.NombreVideojuego, t.OrganizadorId, t.NombreOrganizador, t.FechaInicio, t.FechaFin);
    }

    public Task<IEnumerable<TorneoResumenResponse>> ObtenerPorFechaAsync()
        => _torneoRepo.ObtenerPorFechaAsync();

    public Task<TorneoPorCodigoResponse?> ObtenerPorCodigoAsync(string codigo)
        => _torneoRepo.ObtenerPorCodigoAsync(codigo);

    public async Task<(MutacionResultado Resultado, TorneoResponse? Torneo)> ActualizarAsync(Guid id, EditarTorneoRequest req)
    {
        var t = await _torneoRepo.ObtenerPorIdAsync(id);
        if (t is null) return (MutacionResultado.NoEncontrado, null);

        // Block-on-dependents: el nombre se copia a torneos_por_equipo y a partidas; editar
        // con inscritos/premios dejaría datos inconsistentes en otras particiones/servicios.
        if (await _torneoRepo.TieneDependientesAsync(id))
            return (MutacionResultado.ConDependencias, null);

        var nuevoNombre = req.Nombre.Trim();
        await _torneoRepo.ActualizarAsync(t, nuevoNombre, req.FechaFin);
        return (MutacionResultado.Ok, new TorneoResponse(t.TorneoId, nuevoNombre, t.Codigo, t.VideojuegoId,
            t.NombreVideojuego, t.OrganizadorId, t.NombreOrganizador, t.FechaInicio, req.FechaFin));
    }

    public async Task<MutacionResultado> EliminarAsync(Guid id)
    {
        var t = await _torneoRepo.ObtenerPorIdAsync(id);
        if (t is null) return MutacionResultado.NoEncontrado;

        if (await _torneoRepo.TieneDependientesAsync(id))
            return MutacionResultado.ConDependencias;

        await _torneoRepo.EliminarAsync(t);
        return MutacionResultado.Ok;
    }
}
