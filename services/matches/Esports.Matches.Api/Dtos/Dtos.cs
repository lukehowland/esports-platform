namespace Esports.Matches.Api.Dtos;

public record RegistrarPartidaRequest(
    Guid TorneoId,
    string NombreTorneo,
    Guid EquipoLocalId,
    string NombreLocal,
    Guid EquipoVisitanteId,
    string NombreVisitante,
    Guid EquipoGanadorId,
    string Resultado,
    DateTimeOffset Fecha);

public record PartidaResponse(
    Guid PartidaId,
    Guid TorneoId,
    string NombreTorneo,
    DateTimeOffset Fecha,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    string NombreLocal,
    string NombreVisitante,
    Guid EquipoGanadorId,
    string Resultado);

// Q16
public record PartidaPorTorneoResponse(
    Guid PartidaId,
    string NombreLocal,
    string NombreVisitante,
    string Resultado,
    DateTimeOffset Fecha);

// Q17
public record PartidaPorEquipoResponse(
    Guid PartidaId,
    string NombreTorneo,
    string Rival,
    string Resultado,
    DateTimeOffset Fecha);

// Q18
public record PartidaPorFechaResponse(
    Guid PartidaId,
    Guid TorneoId,
    string NombreLocal,
    string NombreVisitante,
    string Resultado);

// Q19
public record PartidaPorRivalesResponse(
    Guid PartidaId,
    Guid EquipoLocalId,
    string Resultado,
    DateTimeOffset Fecha);
