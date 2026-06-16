namespace Esports.Teams.Api.Domain;

public class Jugador
{
    public Guid JugadorId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public Guid EquipoId { get; set; }
    public DateTimeOffset FechaRegistro { get; set; }
}
