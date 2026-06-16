namespace Esports.Auth.Api.Domain;

/// <summary>
/// Usuario de la plataforma. La identidad demo no usa passwords de producción, pero
/// el hash es real (PBKDF2). Un usuario tiene un rol y, según el rol, queda vinculado
/// a un organizador o a un equipo existente.
/// </summary>
public class Usuario
{
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public required string Rol { get; init; }
    public Guid? OrganizadorId { get; init; }
    public Guid? EquipoId { get; init; }
    public required string NombreDisplay { get; init; }
}
