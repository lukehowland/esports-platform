namespace Esports.Tournaments.Api.Dtos;

// Videojuegos
public record CrearVideojuegoRequest(string Nombre, string Genero);
public record VideojuegoResponse(Guid VideojuegoId, string Nombre, string Genero);
public record VideojuegoPorGeneroResponse(Guid VideojuegoId, string Nombre);

// Organizadores
public record CrearOrganizadorRequest(string Nombre);
public record OrganizadorResponse(Guid OrganizadorId, string Nombre);

// Torneos
public record CrearTorneoRequest(
    string Nombre,
    string Codigo,
    Guid VideojuegoId,
    Guid OrganizadorId,
    DateTimeOffset FechaInicio);

public record TorneoResponse(
    Guid TorneoId,
    string Nombre,
    string Codigo,
    Guid VideojuegoId,
    string NombreVideojuego,
    Guid OrganizadorId,
    string NombreOrganizador,
    DateTimeOffset FechaInicio);

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
public record AsignarPremioRequest(decimal Monto, string Tipo, Guid? EquipoId);
public record PremioResponse(Guid PremioId, Guid TorneoId, decimal Monto, string Tipo, Guid? EquipoId, string? NombreEquipo);
public record PremioEquipoResponse(Guid PremioId, Guid TorneoId, string NombreTorneo, decimal Monto, string Tipo);
