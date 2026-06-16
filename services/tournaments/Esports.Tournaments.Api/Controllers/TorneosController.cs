using Esports.Auth.Shared;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api/torneos")]
public class TorneosController : ControllerBase
{
    private readonly ITorneoService _svc;
    public TorneosController(ITorneoService svc) => _svc = svc;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Crear([FromBody] CrearTorneoRequest req)
    {
        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Organizador || User.GetOrganizadorId() != req.OrganizadorId)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Un organizador solo puede crear torneos con su propio OrganizadorId.");
        }

        try
        {
            var result = await _svc.CrearAsync(req);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = result.TorneoId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { title = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var result = await _svc.ObtenerPorIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // Q12: torneos por fecha de inicio (más reciente primero)
    [HttpGet("por-fecha")]
    public async Task<IActionResult> PorFecha()
        => Ok(await _svc.ObtenerPorFechaAsync());

    // Q15: buscar torneo por código único
    [HttpGet("por-codigo/{codigo}")]
    public async Task<IActionResult> PorCodigo(string codigo)
    {
        var result = await _svc.ObtenerPorCodigoAsync(codigo);
        return result is null ? NotFound() : Ok(result);
    }
}
