namespace Esports.Tournaments.Api.Domain;

public class Videojuego
{
    public Guid VideojuegoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
}

public class Organizador
{
    public Guid OrganizadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Torneo
{
    public Guid TorneoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public Guid VideojuegoId { get; set; }
    public string NombreVideojuego { get; set; } = string.Empty;
    public Guid OrganizadorId { get; set; }
    public string NombreOrganizador { get; set; } = string.Empty;
    public DateTimeOffset FechaInicio { get; set; }
    public DateTimeOffset FechaFin { get; set; }
}

public class Premio
{
    public Guid PremioId { get; set; }
    public Guid TorneoId { get; set; }
    public string NombreTorneo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public Guid? EquipoId { get; set; }
    public string? NombreEquipo { get; set; }
}
