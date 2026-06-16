using Esports.Auth.Shared;
using Esports.Matches.Api.Clients;
using Esports.Matches.Api.Dtos;
using Esports.Matches.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Matches.Api.Controllers;

[ApiController]
[Route("api/partidas")]
public class PartidasController : ControllerBase
{
    private readonly IPartidaService _svc;
    private readonly TournamentsClient _tournamentsClient;

    public PartidasController(IPartidaService svc, TournamentsClient tournamentsClient)
    {
        _svc = svc;
        _tournamentsClient = tournamentsClient;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPartidaRequest req)
    {
        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Organizador)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo organizadores o administradores pueden registrar partidas.");

            var torneo = await _tournamentsClient.ObtenerTorneoAsync(req.TorneoId);
            if (torneo is null)
                return NotFound(new ProblemDetails
                {
                    Title = "Torneo no encontrado",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No se encontró el torneo {req.TorneoId} al verificar autorización.",
                });

            if (torneo.OrganizadorId != User.GetOrganizadorId())
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo el organizador dueño del torneo puede registrar sus partidas.");
        }

        try
        {
            var result = await _svc.RegistrarAsync(req);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = result.PartidaId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var result = await _svc.ObtenerPorIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // Q16: partidas de un torneo (cronológico)
    [HttpGet("por-torneo/{torneoId:guid}")]
    public async Task<IActionResult> PorTorneo(Guid torneoId)
        => Ok(await _svc.ObtenerPorTorneoAsync(torneoId));

    // Q17: historial de partidas de un equipo
    [HttpGet("por-equipo/{equipoId:guid}")]
    public async Task<IActionResult> PorEquipo(Guid equipoId)
        => Ok(await _svc.ObtenerPorEquipoAsync(equipoId));

    // Q18: partidas jugadas en una fecha (YYYY-MM-DD)
    [HttpGet("por-fecha/{dia}")]
    public async Task<IActionResult> PorFecha(string dia)
    {
        try
        {
            return Ok(await _svc.ObtenerPorFechaAsync(dia));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }

    // Q19: enfrentamientos directos entre dos equipos
    [HttpGet("entre/{equipoId:guid}/{rivalId:guid}")]
    public async Task<IActionResult> PorRivales(Guid equipoId, Guid rivalId)
        => Ok(await _svc.ObtenerPorRivalesAsync(equipoId, rivalId));
}
