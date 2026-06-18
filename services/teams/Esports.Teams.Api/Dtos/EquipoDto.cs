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
    string Codigo,
    string Nickname,
    string Nombre,
    string Pais,
    string Rol,
    Guid? EquipoId);

// RF-03: una entrada del historial de equipos de un jugador (activa = FechaHasta null).
public record MembresiaResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Tag,
    string Rol,
    DateTimeOffset FechaDesde,
    DateTimeOffset? FechaHasta,
    bool Activa);

// RF-03: fichar/transferir un jugador a un equipo destino.
public record AsignarJugadorRequest(
    [Required] Guid EquipoDestinoId,
    string? Rol);
