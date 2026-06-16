using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Teams.Api.Controllers;

[ApiController]
[Route("api/equipos")]
public class EquiposController : ControllerBase
{
    private readonly IEquipoService _service;

    public EquiposController(IEquipoService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> CrearEquipo([FromBody] CrearEquipoRequest request)
    {
        var result = await _service.CrearEquipoAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { equipoId = result.EquipoId }, result);
    }

    [HttpGet("{equipoId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid equipoId)
    {
        var equipo = await _service.ObtenerPorIdAsync(equipoId);
        if (equipo is null) return NotFound();
        return Ok(equipo);
    }

    [HttpPost("{equipoId:guid}/jugadores")]
    public async Task<IActionResult> AgregarJugador(Guid equipoId, [FromBody] AgregarJugadorRequest request)
    {
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
