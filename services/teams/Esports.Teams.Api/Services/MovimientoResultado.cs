namespace Esports.Teams.Api.Services;

/// <summary>Resultado de asignar/fichar un jugador a un equipo. El controller lo mapea a HTTP.</summary>
public enum AsignacionResultado
{
    Ok,                    // 200 — fichado o transferido (admin)
    JugadorNoEncontrado,   // 404
    EquipoNoEncontrado,    // 404
    YaEnEseEquipo,         // 409 — el destino ya es su equipo activo
    RequiereLiberar,       // 409 — tiene equipo activo y el actor (capitán) no puede transferir
}

/// <summary>Resultado de liberar (dar de baja) a un jugador de su equipo actual.</summary>
public enum LiberacionResultado
{
    Ok,                    // 200 — liberado, queda agente libre
    JugadorNoEncontrado,   // 404
    YaEsAgenteLibre,       // 409 — no tenía equipo activo
}

/// <summary>Resultado de eliminar (hard-delete) a un jugador. Solo agentes libres.</summary>
public enum EliminacionResultado
{
    Ok,                    // 204
    NoEncontrado,          // 404
    TieneEquipoActivo,     // 409 — debe liberarse primero
}
