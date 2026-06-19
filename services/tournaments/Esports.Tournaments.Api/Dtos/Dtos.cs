using System.ComponentModel.DataAnnotations;

namespace Esports.Tournaments.Api.Dtos;

// Videojuegos
public record CrearVideojuegoRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string Nombre,
    [Required, RegularExpression(@".*\S.*"), MaxLength(40)] string Genero,
    [Required, RegularExpression(@".*\S.*"), MaxLength(40)] string Plataforma);
public record EditarVideojuegoRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string Nombre,
    [Required, RegularExpression(@".*\S.*"), MaxLength(40)] string Genero,
    [Required, RegularExpression(@".*\S.*"), MaxLength(40)] string Plataforma);
public record VideojuegoResponse(Guid VideojuegoId, string Nombre, string Genero, string Plataforma);
public record VideojuegoPorGeneroResponse(Guid VideojuegoId, string Nombre, string Plataforma);

// Organizadores
public record CrearOrganizadorRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string Nombre,
    [Required, EmailAddress, MaxLength(160)] string Email);
public record EditarOrganizadorRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string Nombre,
    [Required, EmailAddress, MaxLength(160)] string Email);
public record OrganizadorResponse(Guid OrganizadorId, string Nombre, string Email);

// Torneos
public record CrearTorneoRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(160)] string Nombre,
    [Required, RegularExpression(@".*\S.*"), MaxLength(32)] string Codigo,
    Guid VideojuegoId,
    Guid OrganizadorId,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin);

public record EditarTorneoRequest(
    [Required, RegularExpression(@".*\S.*"), MaxLength(160)] string Nombre,
    DateTimeOffset FechaFin);

public record TorneoResponse(
    Guid TorneoId,
    string Nombre,
    string Codigo,
    Guid VideojuegoId,
    string NombreVideojuego,
    Guid OrganizadorId,
    string NombreOrganizador,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin);

public record TorneoResumenResponse(
    Guid TorneoId,
    string NombreTorneo,
    string NombreVideojuego,
    DateTimeOffset FechaInicio);

public record TorneoPorCodigoResponse(Guid TorneoId, string Nombre, DateTimeOffset FechaInicio);

// Q9: torneos por videojuego (incluye organizador, no videojuego —  ya se sabe cuál es)
public record TorneoPorVideojuegoResponse(Guid TorneoId, string NombreTorneo, string NombreOrganizador, DateTimeOffset FechaInicio);

// Inscripciones
public record InscribirEquipoRequest(Guid EquipoId);
public record EquipoPorTorneoResponse(Guid EquipoId, string NombreEquipo, DateTimeOffset FechaInscripcion);
public record TorneoPorEquipoResponse(Guid TorneoId, string NombreTorneo, string NombreVideojuego, DateTimeOffset FechaInicio);

// Premios
public record AsignarPremioRequest(
    [Range(0.01, 999999999)] decimal Monto,
    [Required, RegularExpression(@".*\S.*"), MaxLength(80)] string Tipo,
    Guid? EquipoId);
public record PremioResponse(Guid PremioId, Guid TorneoId, decimal Monto, string Tipo, Guid? EquipoId, string? NombreEquipo);
public record PremioEquipoResponse(Guid PremioId, Guid TorneoId, string NombreTorneo, decimal Monto, string Tipo);
