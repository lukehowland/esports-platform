using System.ComponentModel.DataAnnotations;

namespace Esports.Teams.Api.Dtos;

public record CrearEquipoRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string Nombre,
    [Required, RegularExpression(@".*\S.*"), MaxLength(16)] string Tag,
    [Required, RegularExpression(@".*\S.*"), MaxLength(32)] string Pais);

public record EquipoResponse(
    Guid EquipoId,
    string Nombre,
    string Tag,
    string Pais,
    DateTimeOffset FechaCreacion);

public record AgregarJugadorRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(64)] string Nickname,
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string Nombre,
    [Required, RegularExpression(@".*\S.*"), MaxLength(32)] string Pais,
    [Required, RegularExpression(@".*\S.*"), MaxLength(32)] string Rol);

public record JugadorResponse(
    Guid JugadorId,
    string Nickname,
    string Nombre,
    string Pais,
    string Rol,
    Guid EquipoId);
