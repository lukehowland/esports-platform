using System.ComponentModel.DataAnnotations;

namespace Esports.Auth.Api.Dtos;

public record LoginRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(64)] string Username,
    [Required, MaxLength(128)] string Password);

public record LoginResponse(
    string Token,
    string Rol,
    string Nombre,
    Guid? OrganizadorId,
    Guid? EquipoId,
    DateTimeOffset ExpiraEn);

public record RegisterRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(64)] string Username,
    [Required, MaxLength(128)] string Password,
    [Required, RegularExpression(@".*\S.*"), MaxLength(32)] string Rol,
    Guid? OrganizadorId,
    Guid? EquipoId,
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string NombreDisplay);

public record MeResponse(
    string Username,
    string Rol,
    string? Nombre,
    Guid? OrganizadorId,
    Guid? EquipoId);

public record UsuarioResumenResponse(
    string Username,
    string Rol,
    string Nombre,
    Guid? OrganizadorId,
    Guid? EquipoId);
