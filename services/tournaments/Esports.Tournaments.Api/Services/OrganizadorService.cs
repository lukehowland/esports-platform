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
}

public class OrganizadorService : IOrganizadorService
{
    private readonly IOrganizadorRepository _repo;

    public OrganizadorService(IOrganizadorRepository repo) => _repo = repo;

    public async Task<OrganizadorResponse> CrearAsync(CrearOrganizadorRequest req)
    {
        var o = new Organizador { OrganizadorId = Guid.NewGuid(), Nombre = req.Nombre.Trim() };
        await _repo.CrearAsync(o);
        return new OrganizadorResponse(o.OrganizadorId, o.Nombre);
    }

    public async Task<OrganizadorResponse?> ObtenerPorIdAsync(Guid id)
    {
        var o = await _repo.ObtenerPorIdAsync(id);
        return o is null ? null : new OrganizadorResponse(o.OrganizadorId, o.Nombre);
    }

    public Task<IEnumerable<OrganizadorResponse>> ObtenerTodosAsync()
        => _repo.ObtenerTodosAsync();

    public Task<IEnumerable<TorneoResumenResponse>> ObtenerTorneosAsync(Guid organizadorId)
        => _repo.ObtenerTorneosAsync(organizadorId);
}
