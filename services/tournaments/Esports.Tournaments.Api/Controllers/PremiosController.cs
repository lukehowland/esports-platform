using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api")]
public class PremiosController : ControllerBase
{
    private readonly IPremioService _svc;
    public PremiosController(IPremioService svc) => _svc = svc;

    // Q20: asignar / listar premios de un torneo
    [HttpPost("torneos/{torneoId:guid}/premios")]
    public async Task<IActionResult> Asignar(Guid torneoId, [FromBody] AsignarPremioRequest req)
    {
        try
        {
            var result = await _svc.AsignarAsync(torneoId, req);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { title = ex.Message });
        }
    }

    [HttpGet("torneos/{torneoId:guid}/premios")]
    public async Task<IActionResult> PorTorneo(Guid torneoId)
        => Ok(await _svc.ObtenerPorTorneoAsync(torneoId));

    // Q21: premios recibidos por un equipo
    [HttpGet("premios/por-equipo/{equipoId:guid}")]
    public async Task<IActionResult> PorEquipo(Guid equipoId)
        => Ok(await _svc.ObtenerPorEquipoAsync(equipoId));
}
