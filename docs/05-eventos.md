# 05 — Eventos (RabbitMQ + MassTransit v8)

## Por qué eventos

REST sirve cuando un servicio necesita un dato de otro **ahora**. Pero para *reaccionar* a un hecho sin acoplar productor y consumidor, se usan eventos. Conceptos que demuestra en la materia: **desacople** (Tournaments y Matches no saben que existe Ranking), **consistencia eventual** (los rankings/stats se actualizan poco después, no en la misma transacción) y **CQRS** (Ranking es un read-model agregado, alimentado por eventos).

## Broker y librería

- Broker: **RabbitMQ** (imagen `rabbitmq:3-management`, UI en `:15672`, `guest`/`guest`).
- Librería: **MassTransit 8.x** con transporte RabbitMQ.
  - ⚠️ **Pinear `Version="8.*"`**. NO instalar 9.x (comercial, requiere licencia de pago, rompería el build).
  - Paquete: `MassTransit.RabbitMQ`.
- MassTransit declara exchanges y colas automáticamente a partir de los tipos de mensaje y los consumers registrados. No hay que crear colas a mano.

## Contratos de evento (en `Esports.Shared.Events`)

`record` inmutables compartidos por productor y consumidor (ambos referencian el mismo tipo desde `Esports.Shared`).

```csharp
namespace Esports.Shared.Events;

// Publicado por Tournaments al inscribir un equipo en un torneo.
// jugadorIds lleva el roster del equipo en ese momento (event-carried state transfer)
// para que Ranking pueda actualizar Q23 sin un REST extra.
public record TeamRegisteredToTournament(
    Guid EquipoId,
    Guid TorneoId,
    string NombreEquipo,
    IReadOnlyList<Guid> JugadorIds,
    DateTimeOffset FechaInscripcion);

// Publicado por Matches al registrar una partida.
public record MatchPlayed(
    Guid PartidaId,
    Guid TorneoId,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    Guid EquipoGanadorId,
    DateTimeOffset Fecha);
```

### Catálogo

| Evento | Publica | Consume | Efecto en Ranking |
|---|---|---|---|
| `TeamRegisteredToTournament` | tournaments (al inscribir) | ranking | `ranking_equipos_global.total_torneos += 1` del equipo (Q7); `ranking_jugadores_activos.total_torneos += 1` por cada `jugadorId` del roster (Q23) |
| `MatchPlayed` | matches (al registrar partida) | ranking | `ranking_victorias.total_victorias += 1` del ganador (Q22); en `stats_equipo_por_torneo`: ganador `victorias+1, partidas_jugadas+1` y perdedor `derrotas+1, partidas_jugadas+1` (Q24) |

> Estos dos eventos alimentan las 4 tablas del servicio ranking. Es el núcleo event-driven del proyecto.

## Publisher (ejemplo: Tournaments)

Registro en `Program.cs`:
```csharp
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});
```
Publicar dentro del flujo de inscripción, después del `BATCH` exitoso:
```csharp
await _publishEndpoint.Publish(new TeamRegisteredToTournament(
    equipoId, torneoId, nombreEquipo, jugadorIds, DateTimeOffset.UtcNow));
```
Matches hace lo mismo con `MatchPlayed` después de registrar la partida.

## Consumer (Ranking)

Dos consumers, uno por evento. Ejemplo del de inscripción:
```csharp
public class TeamRegisteredConsumer : IConsumer<TeamRegisteredToTournament>
{
    private readonly IRankingRepository _repo;
    private readonly ILogger<TeamRegisteredConsumer> _logger;

    public TeamRegisteredConsumer(IRankingRepository repo, ILogger<TeamRegisteredConsumer> logger)
        => (_repo, _logger) = (repo, logger);

    public async Task Consume(ConsumeContext<TeamRegisteredToTournament> ctx)
    {
        var m = ctx.Message;
        await _repo.IncrementarTorneosEquipoAsync("GLOBAL", m.EquipoId);          // Q7
        foreach (var jugadorId in m.JugadorIds)
            await _repo.IncrementarTorneosJugadorAsync("GLOBAL", jugadorId);      // Q23
        _logger.LogInformation("Ranking torneos++ equipo {EquipoId} (+{N} jugadores)", m.EquipoId, m.JugadorIds.Count);
    }
}
```
El de partidas (`MatchPlayedConsumer`) incrementa victorias del ganador (Q22) y actualiza las stats de ambos equipos en el torneo (Q24).

Registro en `Program.cs` del Ranking:
```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TeamRegisteredConsumer>();
    x.AddConsumer<MatchPlayedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h => { h.Username("guest"); h.Password("guest"); });
        cfg.ConfigureEndpoints(context);
    });
});
```
Repo del Ranking (todo con counters, atómico):
```csharp
// UPDATE ranking_equipos_global    SET total_torneos   = total_torneos   + 1 WHERE bucket=? AND equipo_id=?;
// UPDATE ranking_jugadores_activos SET total_torneos   = total_torneos   + 1 WHERE bucket=? AND jugador_id=?;
// UPDATE ranking_victorias         SET total_victorias = total_victorias + 1 WHERE bucket=? AND equipo_id=?;
// UPDATE stats_equipo_por_torneo   SET victorias = victorias + 1, partidas_jugadas = partidas_jugadas + 1 WHERE equipo_id=? AND torneo_id=?;
// UPDATE stats_equipo_por_torneo   SET derrotas  = derrotas  + 1, partidas_jugadas = partidas_jugadas + 1 WHERE equipo_id=? AND torneo_id=?;
```

## Idempotencia y orden

- Los counters (`+ 1`) son atómicos en Cassandra. RabbitMQ entrega *at-least-once*: si un evento se reentrega, el counter podría inflarse. Para una demo es aceptable.
- Mitigación opcional (si sobra tiempo): guardar los `partida_id`/`inscripcion_id` ya procesados en una tabla `eventos_procesados` y saltar duplicados en el consumer.
- No dependemos del orden entre eventos distintos: cada uno incrementa un counter independiente.

## Verificación rápida (smoke test del evento)

1. Crear equipo con 2-3 jugadores, y un torneo.
2. Inscribir el equipo (`POST /api/torneos/{id}/inscripciones`).
3. En RabbitMQ (`:15672`) ver que pasó el mensaje.
4. `GET /api/ranking/equipos?top=10` → el equipo aparece con `totalTorneos ≥ 1`; `GET /api/ranking/jugadores?top=10` → sus jugadores aparecen.
5. Registrar una partida con ganador → `GET /api/ranking/victorias?top=10` y `GET /api/stats/equipo/{id}/torneo/{id}` reflejan el resultado.
