# 05 — Eventos (RabbitMQ + MassTransit v8)

## Por qué eventos

REST sirve cuando un servicio necesita un dato de otro **ahora**. Pero para *reaccionar* a un hecho sin acoplar productor y consumidor, se usan eventos. Ventajas que demuestra esto en la materia: **desacople** (Tournaments no sabe que existe Ranking), **consistencia eventual** (el ranking se actualiza poco después, no en la misma transacción) y **extensibilidad** (mañana otro servicio se suscribe al mismo evento sin tocar a Tournaments).

## Broker y librería

- Broker: **RabbitMQ** (imagen `rabbitmq:3-management`, UI en `:15672`).
- Librería: **MassTransit 8.x** con el transporte RabbitMQ.
  - ⚠️ **Pinear `Version="8.*"`**. NO instalar 9.x (comercial, requiere licencia de pago y rompería el build).
  - Paquete: `MassTransit.RabbitMQ`.
- MassTransit se encarga de declarar exchanges y colas automáticamente a partir de los tipos de mensaje y los consumers registrados. No hay que crear colas a mano.

## Contratos de evento (en `Esports.Shared.Events`)

Los eventos son `record` inmutables compartidos por productor y consumidor. Viven en el proyecto `Esports.Shared` para que ambos referencien el mismo tipo.

```csharp
namespace Esports.Shared.Events;

// Obligatorio: dispara la actualización del ranking
public record TeamRegisteredToTournament(
    Guid EquipoId,
    Guid TorneoId,
    string NombreEquipo,
    DateTimeOffset FechaInscripcion);
```

### Catálogo

| Evento | Publica | Consume | Efecto |
|---|---|---|---|
| `TeamRegisteredToTournament` | Tournaments (al inscribir un equipo) | Ranking | `total_torneos += 1` para ese equipo en `ranking_equipos_global` |

**Eventos opcionales** (solo si sobra tiempo — no son necesarios para la nota):

| Evento | Publica | Consume | Efecto |
|---|---|---|---|
| `TeamCreated(EquipoId, Nombre, Pais)` | Teams | Ranking | crear la fila del equipo en el ranking con total en 0 (si no se prefiere crearla lazy al primer incremento) |
| `TeamRenamed(EquipoId, NuevoNombre)` | Teams | Tournaments/Matches | refrescar `nombre_equipo` desnormalizado |

> Para el alcance del miércoles: implementar **solo `TeamRegisteredToTournament`**. Es suficiente para demostrar arquitectura event-driven de punta a punta.

## Publisher (Tournaments)

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
Publicar dentro del flujo de inscripción (después del `BATCH` exitoso):
```csharp
await _publishEndpoint.Publish(new TeamRegisteredToTournament(
    equipoId, torneoId, nombreEquipo, DateTimeOffset.UtcNow));
```

## Consumer (Ranking)

```csharp
public class TeamRegisteredConsumer : IConsumer<TeamRegisteredToTournament>
{
    private readonly IRankingRepository _repo;
    private readonly ILogger<TeamRegisteredConsumer> _logger;

    public TeamRegisteredConsumer(IRankingRepository repo, ILogger<TeamRegisteredConsumer> logger)
        => (_repo, _logger) = (repo, logger);

    public async Task Consume(ConsumeContext<TeamRegisteredToTournament> ctx)
    {
        var msg = ctx.Message;
        await _repo.IncrementarTotalTorneosAsync("GLOBAL", msg.EquipoId);
        _logger.LogInformation("Ranking++ equipo {EquipoId}", msg.EquipoId);
    }
}
```
Registro en `Program.cs` del Ranking:
```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TeamRegisteredConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h => { h.Username("guest"); h.Password("guest"); });
        cfg.ConfigureEndpoints(context);
    });
});
```
Repo del Ranking (counter, por eso es idempotente-ish y atómico):
```csharp
// UPDATE ranking_equipos_global SET total_torneos = total_torneos + 1
// WHERE bucket = ? AND equipo_id = ?;
```

## Idempotencia y orden

- El counter (`total_torneos + 1`) es atómico en Cassandra. Si el mismo evento se entrega dos veces (RabbitMQ garantiza *at-least-once*), el conteo podría inflarse. Para una demo es aceptable. Si quieren robustez extra (opcional): guardar los `inscripcion_id` ya procesados y saltar duplicados, o modelar la inscripción como insert idempotente y derivar el total con `COUNT` bajo demanda.
- El orden entre eventos distintos no está garantizado, pero acá solo hay un tipo de evento que incrementa, así que no importa.

## Verificación rápida (smoke test del evento)

1. Crear un equipo (`POST /api/equipos`) y un torneo (`POST /api/torneos`).
2. Inscribir el equipo (`POST /api/torneos/{id}/inscripciones`).
3. En RabbitMQ management (`:15672`) ver que pasó un mensaje por la cola del consumer.
4. `GET /api/ranking/global?top=10` → el equipo aparece con `totalTorneos` ≥ 1.
