using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api/organizadores")]
public class OrganizadoresController : ControllerBase
{
    private readonly IOrganizadorService _svc;
    public OrganizadoresController(IOrganizadorService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearOrganizadorRequest req)
    {
        var result = await _svc.CrearAsync(req);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = result.OrganizadorId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var result = await _svc.ObtenerPorIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // Q10: lista de organizadores
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
        => Ok(await _svc.ObtenerTodosAsync());

    // Q11: torneos de un organizador
    [HttpGet("{id:guid}/torneos")]
    public async Task<IActionResult> Torneos(Guid id)
        => Ok(await _svc.ObtenerTorneosAsync(id));
}
