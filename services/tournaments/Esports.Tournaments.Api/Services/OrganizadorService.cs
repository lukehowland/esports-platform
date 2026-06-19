using Esports.Tournaments.Api.Domain;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Repositories;

namespace Esports.Tournaments.Api.Services;

public interface IOrganizadorService
{
    Task<OrganizadorResponse> CrearAsync(CrearOrganizadorRequest req);
    Task<OrganizadorResponse?> ObtenerPorIdAsync(Guid id);
    Task<IEnumerable<OrganizadorResponse>> ObtenerTodosAsync();
    Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid organizadorId);
    Task<MutacionResultado> ActualizarAsync(Guid id, EditarOrganizadorRequest req);
    Task<MutacionResultado> EliminarAsync(Guid id);
}

public class OrganizadorService : IOrganizadorService
{
    private readonly IOrganizadorRepository _repo;

    public OrganizadorService(IOrganizadorRepository repo) => _repo = repo;

    public async Task<OrganizadorResponse> CrearAsync(CrearOrganizadorRequest req)
    {
        var o = new Organizador { OrganizadorId = Guid.NewGuid(), Nombre = req.Nombre.Trim(), Email = req.Email.Trim() };
        await _repo.CrearAsync(o);
        return new OrganizadorResponse(o.OrganizadorId, o.Nombre, o.Email);
    }

    public async Task<OrganizadorResponse?> ObtenerPorIdAsync(Guid id)
    {
        var o = await _repo.ObtenerPorIdAsync(id);
        return o is null ? null : new OrganizadorResponse(o.OrganizadorId, o.Nombre, o.Email);
    }

    public Task<IEnumerable<OrganizadorResponse>> ObtenerTodosAsync()
        => _repo.ObtenerTodosAsync();

    public Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid organizadorId)
        => _repo.ObtenerTorneosAsync(organizadorId);

    public async Task<MutacionResultado> ActualizarAsync(Guid id, EditarOrganizadorRequest req)
    {
        if (await _repo.ObtenerPorIdAsync(id) is null)
            return MutacionResultado.NoEncontrado;

        // Block-on-dependents: el nombre se copia a las tablas de torneos; renombrar con
        // torneos asociados dejaría datos inconsistentes en otras particiones.
        if (await _repo.TieneTorneosAsync(id))
            return MutacionResultado.ConDependencias;

        await _repo.ActualizarAsync(id, req.Nombre.Trim(), req.Email.Trim());
        return MutacionResultado.Ok;
    }

    public async Task<MutacionResultado> EliminarAsync(Guid id)
    {
        if (await _repo.ObtenerPorIdAsync(id) is null)
            return MutacionResultado.NoEncontrado;

        if (await _repo.TieneTorneosAsync(id))
            return MutacionResultado.ConDependencias;

        await _repo.EliminarAsync(id);
        return MutacionResultado.Ok;
    }
}
