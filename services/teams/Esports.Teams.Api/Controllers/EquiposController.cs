using Esports.Auth.Shared;
using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Teams.Api.Controllers;

[ApiController]
[Route("api/equipos")]
public class EquiposController : ControllerBase
{
    private readonly IEquipoService _service;

    public EquiposController(IEquipoService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
    public async Task<IActionResult> CrearEquipo([FromBody] CrearEquipoRequest request)
    {
        var result = await _service.CrearEquipoAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { equipoId = result.EquipoId }, result);
    }

    // RF-02: editar equipo (admin; bloqueado si tiene roster)
    [HttpPut("{equipoId:guid}")]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
    public async Task<IActionResult> EditarEquipo(Guid equipoId, [FromBody] EditarEquipoRequest request)
    {
        return await _service.ActualizarEquipoAsync(equipoId, request) switch
        {
            MutacionResultado.Ok => Ok(await _service.ObtenerPorIdAsync(equipoId)),
            MutacionResultado.NoEncontrado => NotFound(),
            MutacionResultado.ConDependencias => Problem(
                title: "Equipo con roster",
                statusCode: StatusCodes.Status409Conflict,
                detail: "No se puede editar un equipo que tiene jugadores. Liberá su roster primero."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // RF-02: eliminar equipo (admin; bloqueado si tiene roster)
    [HttpDelete("{equipoId:guid}")]
    [Authorize(Roles = AuthConstants.Roles.Admin)]
    public async Task<IActionResult> EliminarEquipo(Guid equipoId)
    {
        return await _service.EliminarEquipoAsync(equipoId) switch
        {
            MutacionResultado.Ok => NoContent(),
            MutacionResultado.NoEncontrado => NotFound(),
            MutacionResultado.ConDependencias => Problem(
                title: "Equipo con roster",
                statusCode: StatusCodes.Status409Conflict,
                detail: "No se puede eliminar un equipo que tiene jugadores. Liberá su roster primero."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{equipoId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid equipoId)
    {
        var equipo = await _service.ObtenerPorIdAsync(equipoId);
        if (equipo is null) return NotFound();
        return Ok(equipo);
    }

    [HttpPost("{equipoId:guid}/jugadores")]
    [Authorize]
    public async Task<IActionResult> AgregarJugador(Guid equipoId, [FromBody] AgregarJugadorRequest request)
    {
        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Capitan || User.GetEquipoId() != equipoId)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo el capitán del equipo puede agregar jugadores.");
        }

        var equipo = await _service.ObtenerPorIdAsync(equipoId);
        if (equipo is null) return NotFound();
        var jugador = await _service.AgregarJugadorAsync(equipoId, request);
        return StatusCode(StatusCodes.Status201Created, jugador);
    }

    // Q4
    [HttpGet("por-fecha")]
    public async Task<IActionResult> ObtenerPorFecha()
    {
        var result = await _service.ObtenerPorFechaAsync();
        return Ok(result);
    }

    // Q5
    [HttpGet("por-tag/{tag}")]
    public async Task<IActionResult> ObtenerPorTag(string tag)
    {
        var equipo = await _service.ObtenerPorTagAsync(tag);
        if (equipo is null) return NotFound();
        return Ok(equipo);
    }

    // Q3
    [HttpGet("{equipoId:guid}/jugadores")]
    public async Task<IActionResult> ObtenerJugadores(Guid equipoId, [FromQuery] string? pais)
    {
        var jugadores = await _service.ObtenerJugadoresPorEquipoAsync(equipoId, pais);
        return Ok(jugadores);
    }

    // Q6
    [HttpGet("{equipoId:guid}/integrantes")]
    public async Task<IActionResult> ObtenerIntegrantes(Guid equipoId)
    {
        var integrantes = await _service.ObtenerIntegrantesAsync(equipoId);
        return Ok(integrantes);
    }
}
