using Esports.Auth.Shared;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api")]
public class PremiosController : ControllerBase
{
    private readonly IPremioService _svc;
    private readonly ITorneoService _torneoSvc;

    public PremiosController(IPremioService svc, ITorneoService torneoSvc)
    {
        _svc = svc;
        _torneoSvc = torneoSvc;
    }

    // Q20: asignar / listar premios de un torneo
    [HttpPost("torneos/{torneoId:guid}/premios")]
    [Authorize]
    public async Task<IActionResult> Asignar(Guid torneoId, [FromBody] AsignarPremioRequest req)
    {
        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Organizador)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo organizadores o administradores pueden asignar premios.");

            var torneo = await _torneoSvc.ObtenerPorIdAsync(torneoId);
            if (torneo is null) return NotFound();

            if (torneo.OrganizadorId != User.GetOrganizadorId())
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo el organizador dueño del torneo puede asignar premios.");
        }

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
