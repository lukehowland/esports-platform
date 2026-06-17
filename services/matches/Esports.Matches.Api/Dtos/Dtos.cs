using System.ComponentModel.DataAnnotations;

namespace Esports.Matches.Api.Dtos;

public record RegistrarPartidaRequest(
    Guid TorneoId,
    [Required, RegularExpression(@".*\S.*"), MaxLength(160)] string NombreTorneo,
    Guid EquipoLocalId,
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string NombreLocal,
    Guid EquipoVisitanteId,
    [Required, RegularExpression(@".*\S.*"), MaxLength(120)] string NombreVisitante,
    Guid EquipoGanadorId,
    [Required, RegularExpression(@".*\S.*"), MaxLength(80)] string Resultado,
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

public record LiveTeamState(
    string EquipoId,
    string Nombre,
    string Tag,
    string Pais,
    int Kills,
    int Torres,
    int Dragones,
    int Barones,
    int Oro,
    int OroPorMinuto,
    bool VaGanando);

public record LiveObjectiveEvent(
    int Segundo,
    string Minuto,
    string Tipo,
    string Nombre,
    string EquipoTag);

public record LiveTimelineEvent(
    int Segundo,
    string Minuto,
    string EquipoTag,
    string Tipo,
    string Texto,
    int LocalKills,
    int VisitanteKills);

public record LiveMatchResponse(
    string MatchId,
    string Estado,
    int DuracionSegundos,
    int SegundoActual,
    string Reloj,
    string Videojuego,
    string TorneoCodigo,
    string TorneoNombre,
    LiveTeamState Local,
    LiveTeamState Visitante,
    IReadOnlyList<LiveObjectiveEvent> Objetivos,
    IReadOnlyList<LiveTimelineEvent> Timeline,
    string Narrativa);
