using Esports.Auth.Shared;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api")]
public class InscripcionesController : ControllerBase
{
    private readonly IInscripcionService _svc;
    public InscripcionesController(IInscripcionService svc) => _svc = svc;

    // Q13: inscribir equipo + listar equipos por torneo
    [HttpPost("torneos/{torneoId:guid}/inscripciones")]
    [Authorize]
    public async Task<IActionResult> Inscribir(Guid torneoId, [FromBody] InscribirEquipoRequest req)
    {
        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Capitan || User.GetEquipoId() != req.EquipoId)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo el capitán del equipo puede inscribirlo en un torneo.");
        }

        try
        {
            await _svc.InscribirAsync(torneoId, req);
            return StatusCode(201);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { title = ex.Message });
        }
    }

    [HttpGet("torneos/{torneoId:guid}/equipos")]
    public async Task<IActionResult> EquiposPorTorneo(Guid torneoId)
        => Ok(await _svc.ObtenerEquiposPorTorneoAsync(torneoId));

    // Q14: torneos en los que participó un equipo
    [HttpGet("torneos/por-equipo/{equipoId:guid}")]
    public async Task<IActionResult> TorneosPorEquipo(Guid equipoId)
        => Ok(await _svc.ObtenerTorneosPorEquipoAsync(equipoId));
}
