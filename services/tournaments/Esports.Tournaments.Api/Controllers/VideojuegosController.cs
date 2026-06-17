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

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarVideojuegoRequest req)
    {
        if (!PuedeMutar()) return AccesoDenegado();

        var (resultado, videojuego) = await _svc.ActualizarAsync(id, req);
        return resultado switch
        {
            MutacionResultado.Ok => Ok(videojuego),
            MutacionResultado.NoEncontrado => NoEncontrado(id),
            _ => ConDependencias(),
        };
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        if (!PuedeMutar()) return AccesoDenegado();

        var resultado = await _svc.EliminarAsync(id);
        return resultado switch
        {
            MutacionResultado.Ok => NoContent(),
            MutacionResultado.NoEncontrado => NoEncontrado(id),
            _ => ConDependencias(),
        };
    }

    private bool PuedeMutar() => User.EsAdmin() || User.GetRol() == AuthConstants.Roles.Organizador;

    private IActionResult AccesoDenegado() => Problem(
        title: "Acceso denegado",
        statusCode: StatusCodes.Status403Forbidden,
        detail: "Solo organizadores o administradores pueden modificar videojuegos.");

    private IActionResult NoEncontrado(Guid id) => Problem(
        title: "Videojuego no encontrado",
        statusCode: StatusCodes.Status404NotFound,
        detail: $"No existe un videojuego con id {id}.");

    private IActionResult ConDependencias() => Problem(
        title: "Videojuego con torneos asociados",
        statusCode: StatusCodes.Status409Conflict,
        detail: "No se puede editar ni eliminar un videojuego que tiene torneos. Eliminá primero sus torneos.");
}
