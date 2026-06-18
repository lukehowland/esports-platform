using Esports.Auth.Shared;
using Esports.Teams.Api.Dtos;
using Esports.Teams.Api.Services;
using Microsoft.AspNetCore.Authorization;
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

    // RF-03: jugador por id
    [HttpGet("{jugadorId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid jugadorId)
    {
        var jugador = await _service.ObtenerPorIdAsync(jugadorId);
        if (jugador is null) return NotFound();
        return Ok(jugador);
    }

    // RF-03: jugador por código legible (J-001)
    [HttpGet("por-codigo/{codigo}")]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo)
    {
        var jugador = await _service.ObtenerPorCodigoAsync(codigo);
        if (jugador is null) return NotFound();
        return Ok(jugador);
    }

    // RF-03: historial de equipos del jugador (membresías; activa = fechaHasta null)
    [HttpGet("{jugadorId:guid}/membresias")]
    public async Task<IActionResult> ObtenerMembresias(Guid jugadorId)
    {
        var membresias = await _service.ObtenerMembresiasAsync(jugadorId);
        if (membresias is null) return NotFound();
        return Ok(membresias);
    }

    // RF-03: liberar (baja) — admin o capitán del equipo actual del jugador.
    [HttpPost("{jugadorId:guid}/liberar")]
    [Authorize]
    public async Task<IActionResult> Liberar(Guid jugadorId)
    {
        var jugador = await _service.ObtenerPorIdAsync(jugadorId);
        if (jugador is null) return NotFound();

        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Capitan || User.GetEquipoId() != jugador.EquipoId)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo el admin o el capitán del equipo actual puede liberar al jugador.");
        }

        var result = await _service.LiberarAsync(jugadorId);
        return result switch
        {
            LiberacionResultado.Ok => NoContent(),
            LiberacionResultado.JugadorNoEncontrado => NotFound(),
            LiberacionResultado.YaEsAgenteLibre => Problem(
                title: "Sin equipo activo",
                statusCode: StatusCodes.Status409Conflict,
                detail: "El jugador ya es agente libre."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    // RF-03: asignar/fichar — admin (cualquiera, transfiere si hace falta) o capitán del
    // equipo destino (solo si el jugador es agente libre; si no, debe liberarse primero).
    [HttpPost("{jugadorId:guid}/asignar")]
    [Authorize]
    public async Task<IActionResult> Asignar(Guid jugadorId, [FromBody] AsignarJugadorRequest request)
    {
        if (!User.EsAdmin())
        {
            if (User.GetRol() != AuthConstants.Roles.Capitan || User.GetEquipoId() != request.EquipoDestinoId)
                return Problem(
                    title: "Acceso denegado",
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: "Solo el admin o el capitán del equipo destino puede fichar al jugador.");
        }

        var result = await _service.AsignarAsync(jugadorId, request.EquipoDestinoId, request.Rol, User.EsAdmin());
        return result switch
        {
            AsignacionResultado.Ok => NoContent(),
            AsignacionResultado.JugadorNoEncontrado => NotFound(),
            AsignacionResultado.EquipoNoEncontrado => Problem(
                title: "Equipo no encontrado",
                statusCode: StatusCodes.Status404NotFound,
                detail: "El equipo destino no existe."),
            AsignacionResultado.YaEnEseEquipo => Problem(
                title: "Jugador ya en el equipo",
                statusCode: StatusCodes.Status409Conflict,
                detail: "El jugador ya pertenece a ese equipo."),
            AsignacionResultado.RequiereLiberar => Problem(
                title: "El jugador tiene equipo activo",
                statusCode: StatusCodes.Status409Conflict,
                detail: "El jugador ya tiene un equipo activo; debe ser liberado antes de fichar."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
