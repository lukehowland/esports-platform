namespace Esports.Teams.Api.Dtos;

public record CrearEquipoRequest(string Nombre, string Tag, string Pais);

public record EquipoResponse(
    Guid EquipoId,
    string Nombre,
    string Tag,
    string Pais,
    DateTimeOffset FechaCreacion);

public record AgregarJugadorRequest(string Nickname, string Nombre, string Pais, string Rol);

public record JugadorResponse(
    Guid JugadorId,
    string Nickname,
    string Nombre,
    string Pais,
    string Rol,
    Guid EquipoId);
