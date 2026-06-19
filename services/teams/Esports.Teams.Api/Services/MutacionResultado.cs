namespace Esports.Teams.Api.Services;

/// <summary>
/// Resultado de editar/eliminar un equipo. El controller lo mapea a 200/204 / 404 / 409.
/// Mismo patrón que el CRUD de organizadores/videojuegos en el servicio tournaments.
/// </summary>
public enum MutacionResultado
{
    Ok,
    NoEncontrado,
    ConDependencias,
}
