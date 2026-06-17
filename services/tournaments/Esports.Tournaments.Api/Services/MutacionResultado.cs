namespace Esports.Tournaments.Api.Services;

/// <summary>
/// Resultado de una mutación (editar/eliminar) sobre una entidad de catálogo.
/// El controller lo mapea a 204 / 404 / 409 con ProblemDetails.
/// </summary>
public enum MutacionResultado
{
    Ok,
    NoEncontrado,
    ConDependencias,
}
