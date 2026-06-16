namespace Esports.Matches.Api.Domain;

public class Partida
{
    public Guid PartidaId { get; set; }
    public Guid TorneoId { get; set; }
    public string NombreTorneo { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; }
    public Guid EquipoLocalId { get; set; }
    public Guid EquipoVisitanteId { get; set; }
    public string NombreLocal { get; set; } = string.Empty;
    public string NombreVisitante { get; set; } = string.Empty;
    public Guid EquipoGanadorId { get; set; }
    public string Resultado { get; set; } = string.Empty;
}
