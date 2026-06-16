using Esports.Ranking.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Esports.Ranking.Api.Controllers;

[ApiController]
[Route("api")]
public class RankingController : ControllerBase
{
    private readonly IRankingRepository _repo;
    public RankingController(IRankingRepository repo) => _repo = repo;

    // Q7: ranking global de equipos por torneos
    [HttpGet("ranking/equipos")]
    public async Task<IActionResult> RankingEquipos([FromQuery] int top = 10)
        => Ok(await _repo.ObtenerRankingEquiposAsync(top));

    // Q22: ranking de equipos por victorias
    [HttpGet("ranking/victorias")]
    public async Task<IActionResult> RankingVictorias([FromQuery] int top = 10)
        => Ok(await _repo.ObtenerRankingVictoriasAsync(top));

    // Q23: jugadores más activos
    [HttpGet("ranking/jugadores")]
    public async Task<IActionResult> RankingJugadores([FromQuery] int top = 10)
        => Ok(await _repo.ObtenerRankingJugadoresAsync(top));

    // Q24: estadísticas de un equipo en un torneo
    [HttpGet("stats/equipo/{equipoId:guid}/torneo/{torneoId:guid}")]
    public async Task<IActionResult> StatsEquipoTorneo(Guid equipoId, Guid torneoId)
    {
        var result = await _repo.ObtenerStatsEquipoTorneoAsync(equipoId, torneoId);
        return result is null ? NotFound() : Ok(result);
    }
}
