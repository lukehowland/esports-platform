namespace Esports.Teams.Api.Domain;

public class Equipo
{
    public Guid EquipoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
}
