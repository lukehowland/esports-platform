using Esports.Teams.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Teams.Api.Controllers;

[ApiController]
[Route("api/jugadores")]
public class JugadoresController : ControllerBase
{
    private readonly IJugadorService _service;

    public JugadoresController(IJugadorService service) => _service = service;

    // Q1
    [HttpGet("por-nickname/{nickname}")]
    public async Task<IActionResult> ObtenerPorNickname(string nickname)
    {
        var jugador = await _service.ObtenerPorNicknameAsync(nickname);
        if (jugador is null) return NotFound();
        return Ok(jugador);
    }

    // Q2
    [HttpGet("por-pais/{pais}")]
    public async Task<IActionResult> ObtenerPorPais(string pais)
    {
        var jugadores = await _service.ObtenerPorPaisAsync(pais);
        return Ok(jugadores);
    }
}
