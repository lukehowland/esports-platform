using Esports.Auth.Shared;
using Esports.Tournaments.Api.Dtos;
using Esports.Tournaments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Tournaments.Api.Controllers;

[ApiController]
[Route("api/videojuegos")]
public class VideojuegosController : ControllerBase
{
    private readonly IVideojuegoService _svc;
    public VideojuegosController(IVideojuegoService svc) => _svc = svc;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Crear([FromBody] CrearVideojuegoRequest req)
    {
        if (!User.EsAdmin() && User.GetRol() != AuthConstants.Roles.Organizador)
            return Problem(
                title: "Acceso denegado",
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Solo organizadores o administradores pueden crear videojuegos.");

        var result = await _svc.CrearAsync(req);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = result.VideojuegoId }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var result = await _svc.ObtenerPorIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // Q8: videojuegos por género
    [HttpGet("por-genero/{genero}")]
    public async Task<IActionResult> PorGenero(string genero)
        => Ok(await _svc.ObtenerPorGeneroAsync(genero));

    // Q9: torneos de un videojuego
    [HttpGet("{id:guid}/torneos")]
    public async Task<IActionResult> Torneos(Guid id)
        => Ok(await _svc.ObtenerTorneosAsync(id));
}
