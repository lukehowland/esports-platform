using System.Security.Claims;

namespace Esports.Auth.Shared;

/// <summary>
/// Helpers para leer los claims de identidad desde los controllers de los servicios.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetRol(this ClaimsPrincipal user)
        => user.FindFirstValue(AuthConstants.Claims.Rol);

    public static bool EsAdmin(this ClaimsPrincipal user)
        => user.GetRol() == AuthConstants.Roles.Admin;

    public static Guid? GetOrganizadorId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue(AuthConstants.Claims.OrganizadorId), out var id) ? id : null;

    public static Guid? GetEquipoId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirstValue(AuthConstants.Claims.EquipoId), out var id) ? id : null;

    public static string? GetNombre(this ClaimsPrincipal user)
        => user.FindFirstValue(AuthConstants.Claims.Nombre);
}
