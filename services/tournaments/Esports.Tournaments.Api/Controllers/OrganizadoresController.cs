using Esports.Auth.Shared;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api/organizadores")]
public class OrganizadoresController : ControllerBase
{
    private readonly IOrganizadorService _svc;
    public OrganizadoresController(IOrganizadorService svc) => _svc = svc;

    [HttpPost]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
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

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarOrganizadorRequest req)
    {
        var resultado = await _svc.ActualizarAsync(id, req);
        return resultado switch
        {
            MutacionResultado.Ok => Ok(new OrganizadorResponse(id, req.Nombre.Trim())),
            MutacionResultado.NoEncontrado => NoEncontrado(id),
            _ => ConDependencias(),
        };
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var resultado = await _svc.EliminarAsync(id);
        return resultado switch
        {
            MutacionResultado.Ok => NoContent(),
            MutacionResultado.NoEncontrado => NoEncontrado(id),
            _ => ConDependencias(),
        };
    }

    private IActionResult NoEncontrado(Guid id) => Problem(
        title: "Organizador no encontrado",
        statusCode: StatusCodes.Status404NotFound,
        detail: $"No existe un organizador con id {id}.");

    private IActionResult ConDependencias() => Problem(
        title: "Organizador con torneos asociados",
        statusCode: StatusCodes.Status409Conflict,
        detail: "No se puede editar ni eliminar un organizador que tiene torneos. Eliminá primero sus torneos.");
}
