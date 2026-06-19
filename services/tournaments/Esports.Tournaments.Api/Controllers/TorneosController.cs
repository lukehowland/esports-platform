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

        if (req.FechaFin < req.FechaInicio)
            return Problem(title: "Fechas inválidas", statusCode: StatusCodes.Status400BadRequest,
                detail: "La fecha de fin no puede ser anterior a la fecha de inicio.");

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

    // RF-06: editar torneo (admin o el organizador dueño; bloqueado si tiene inscritos/premios)
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarTorneoRequest req)
    {
        var torneo = await _svc.ObtenerPorIdAsync(id);
        if (torneo is null) return NotFound();
        if (!EsDueño(torneo.OrganizadorId)) return Prohibido();
        if (req.FechaFin < torneo.FechaInicio)
            return Problem(title: "Fechas inválidas", statusCode: StatusCodes.Status400BadRequest,
                detail: "La fecha de fin no puede ser anterior a la fecha de inicio.");

        var (resultado, actualizado) = await _svc.ActualizarAsync(id, req);
        return resultado switch
        {
            MutacionResultado.Ok => Ok(actualizado),
            MutacionResultado.NoEncontrado => NotFound(),
            MutacionResultado.ConDependencias => Problem(title: "Torneo con dependencias",
                statusCode: StatusCodes.Status409Conflict,
                detail: "No se puede editar un torneo con equipos inscritos o premios asignados."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // RF-06: eliminar torneo (admin o el organizador dueño; bloqueado si tiene inscritos/premios)
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var torneo = await _svc.ObtenerPorIdAsync(id);
        if (torneo is null) return NotFound();
        if (!EsDueño(torneo.OrganizadorId)) return Prohibido();

        return await _svc.EliminarAsync(id) switch
        {
            MutacionResultado.Ok => NoContent(),
            MutacionResultado.NoEncontrado => NotFound(),
            MutacionResultado.ConDependencias => Problem(title: "Torneo con dependencias",
                statusCode: StatusCodes.Status409Conflict,
                detail: "No se puede eliminar un torneo con equipos inscritos o premios asignados."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private bool EsDueño(Guid organizadorId) =>
        User.EsAdmin() || (User.GetRol() == AuthConstants.Roles.Organizador && User.GetOrganizadorId() == organizadorId);

    private IActionResult Prohibido() => Problem(
        title: "Acceso denegado", statusCode: StatusCodes.Status403Forbidden,
        detail: "Solo el admin o el organizador dueño puede gestionar este torneo.");

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
