namespace Esports.Teams.Api.Domain;

/// <summary>
/// RF-03: pertenencia de un jugador a un equipo con validez temporal (entidad asociativa N:N).
/// Activa = <see cref="FechaHasta"/> es null. Una membresía nunca se borra: al liberar al
/// jugador se cierra (se setea FechaHasta), preservando el historial de equipos.
/// </summary>
public class Membresia
{
    public Guid JugadorId { get; set; }
    public Guid EquipoId { get; set; }
    public string NombreEquipo { get; set; } = string.Empty;
    public string TagEquipo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public DateTimeOffset FechaDesde { get; set; }
    public DateTimeOffset? FechaHasta { get; set; }

    public bool Activa => FechaHasta is null;
}
