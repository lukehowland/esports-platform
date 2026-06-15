# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Estado del repo

**Este repo está en etapa de planificación**: solo existen `docs/` (las especificaciones) y este `CLAUDE.md`. Todavía no hay código (.NET, Cassandra, RabbitMQ, etc.). La implementación se hace siguiendo `docs/07-plan-ejecucion.md` fase por fase, empezando por **Fase 0 (scaffolding)**.

**Antes de escribir cualquier código, leé `docs/01` a `docs/06`** — definen arquitectura, modelo de datos, convenciones, contratos de API y eventos exactos que hay que respetar. No inventes nombres de tablas, rutas, namespaces ni tipos de evento: ya están todos decididos en esos docs.

| Doc | Contenido |
|---|---|
| `docs/01-arquitectura.md` | Microservicios, fronteras, REST vs eventos, BATCH/dual-write |
| `docs/02-modelo-datos.md` | DDL completo de Cassandra (Chebotko), mapa query→tabla→servicio |
| `docs/03-convenciones.md` | Casing, idioma, namespaces, estructura de carpetas |
| `docs/04-contratos-api.md` | Endpoints REST exactos (request/response) y ruteo del gateway |
| `docs/05-eventos.md` | Contrato de evento `TeamRegisteredToTournament` (MassTransit) |
| `docs/06-docker-setup.md` | `docker-compose.yml`, Dockerfiles, bootstrap de schema |
| `docs/07-plan-ejecucion.md` | Plan de fases (0–7) para construir el sistema con agentes |
| `docs/08-commits.md` | Reglas de commits atómicos, formato Conventional Commits, en inglés |

## Visión general de la arquitectura

Sistema distribuido de 4 microservicios .NET 10 + un API Gateway (YARP), cada uno con su propio keyspace de Cassandra (**database-per-service**). Comunicación entre servicios: **REST síncrono** (cuando se necesita un dato de otro al momento) y **eventos por RabbitMQ/MassTransit v8** (para reaccionar a hechos sin acoplarse).

| Servicio | Puerto | Keyspace | Dueño de |
|---|---|---|---|
| `teams` | 5002 | `esports_teams` | Equipos, jugadores (Q3, Q10) |
| `tournaments` | 5001 | `esports_tournaments` | Torneos, premios, inscripciones (Q1, Q2, Q5, Q6, Q7) — **publica** `TeamRegisteredToTournament` |
| `matches` | 5003 | `esports_matches` | Partidas (Q4, Q8) |
| `ranking` | 5004 | `esports_ranking` | Ranking global, **solo lectura pública**, event-driven (Q9) — **consume** `TeamRegisteredToTournament` |
| `gateway` | 8080 | — | YARP, única URL pública para el frontend |

**`teams` es la plantilla de oro**: se implementa primero end-to-end y los demás servicios se construyen copiando su estructura de carpetas.

### Flujo end-to-end de referencia (inscribir equipo en torneo)

`POST /api/torneos/{id}/inscripciones` → gateway → `tournaments` pide `nombre_equipo` a `teams` por REST (`HttpClient` tipado, nunca `new HttpClient()`) → `tournaments` escribe `equipos_por_torneo` + `torneos_por_equipo` en un **`BATCH`** → publica `TeamRegisteredToTournament` → `ranking` lo consume async y hace `UPDATE ... total_torneos + 1` (counter). La respuesta 201 no espera ese último paso (eventual consistency).

### Reglas de dual-write

- **Misma base/servicio**: tablas desnormalizadas se escriben juntas en un **`BATCH` (logged)** de CQL.
- **Bases distintas**: nunca BATCH entre keyspaces de servicios distintos — usar REST o eventos.

## Modelo de datos (Cassandra / Chebotko)

Query-first: cada query (Q1–Q10) tiene su propia tabla, con datos desnormalizados a propósito (`nombre_equipo`, `nombre_torneo`, etc. duplicados entre tablas). El DDL completo está en `docs/02-modelo-datos.md`; puntos clave a no romper:

- **`ranking_equipos_global`** usa `PARTITION KEY = bucket` (siempre `'GLOBAL'`), `CLUSTERING KEY = equipo_id`, y `total_torneos` es una columna **`counter`** (no va en la clustering key — es la corrección aplicada al diseño Chebotko original). Como es tabla de counters, **no puede tener columnas no-counter** (por eso no lleva `nombre_equipo`). Top-N = leer toda la partición y ordenar en memoria con `OrderByDescending(...).Take(n)`.
- Cada servicio agrega **tablas base "por id"** (`equipos`, `jugadores`, `torneos`, `partidas`) además de las tablas de consulta — no están en el diagrama Chebotko original pero son requeridas.
- Cada servicio crea **solo el bloque de su propio keyspace**, con `CREATE ... IF NOT EXISTS` (idempotente), al arrancar — vía `Cassandra/SchemaInitializer.cs` con retry por **Polly**.
- RF=1, single-node — es entorno de desarrollo/demo, no producción (no afirmar lo contrario en informes).

## Convenciones (resumen — detalle en `docs/03`)

- **Casing depende del contexto**: C# = `PascalCase` (clases/métodos/props) y `_camelCase` (campos privados); Cassandra/Docker/rutas REST = `snake-case`/`kebab-case` minúscula; eventos MassTransit = `PascalCase`; env vars = `PascalCase__Anidado`.
- **Idioma**: dominio del negocio en **español** (`Torneo`, `Equipo`, `Jugador`, `Partida`, `Premio`, nombres de tablas/columnas/rutas). Capas técnicas en **inglés** (`Repository`, `Service`, `Controller`, `Dto`, `Consumer`, `Publisher`). Ej: `public class TorneoRepository`.
- **Namespaces**: raíz `Esports`, patrón `Esports.<Servicio>.Api` (+ `.Controllers`, `.Domain`, `.Repositories`, `.Services`, `.Dtos`, `.Cassandra`, y `.Events`/`.Consumers` donde aplique). Contratos de eventos compartidos viven en `Esports.Shared.Events`.
- **Estructura por servicio** (un proyecto por servicio, sin Clean Architecture multi-proyecto): `Controllers/`, `Domain/`, `Dtos/`, `Repositories/`, `Services/`, `Cassandra/` (+ `Events/` en tournaments, `Consumers/` en ranking).
- **Código**: todo async (`...Async` → `Task`/`Task<T>`, nunca `.Result`/`.Wait()`); controllers delgados que delegan a Services; Repositories son la única capa que habla CQL (prepared statements, reuso); DTOs separados de Domain; ids `uuid`↔`Guid` generados server-side; fechas `timestamp`↔`DateTimeOffset` en UTC; errores como `ProblemDetails` (400/404/502/503); logging con `ILogger<T>` (nunca `Console.WriteLine`).
- **Config**: `appsettings.json` + env vars (env gana). Secciones estándar: `Cassandra`, `RabbitMq`, `Services`. En Docker, las URLs de `Services__*` usan nombres de servicio de la red Docker (no `localhost`).
- **Commits**: atómicos (un cambio lógico por commit) y **en inglés**, formato Conventional Commits (`type(scope): description`, ej. `feat(teams): add players by country endpoint (Q3)`). Detalle completo en `docs/08-commits.md`.

## Eventos (RabbitMQ + MassTransit v8)

⚠️ **Pinear `MassTransit.RabbitMQ` a `Version="8.*"` — NUNCA 9.x** (es comercial/de pago y rompe el build).

Único evento requerido: `TeamRegisteredToTournament(Guid EquipoId, Guid TorneoId, string NombreEquipo, DateTimeOffset FechaInscripcion)` en `Esports.Shared.Events`. Publica `tournaments` al inscribir un equipo (después del BATCH); consume `ranking` para incrementar el counter `total_torneos`. Detalle completo y snippets en `docs/05-eventos.md`.

## Comandos (una vez exista el código)

```bash
# Levantar toda la plataforma (Cassandra, RabbitMQ, los 4 servicios, gateway)
docker compose up --build

# Reset total (borra el volumen de Cassandra)
docker compose down -v

# Build de un proyecto individual
dotnet build services/teams/Esports.Teams.Api

# Correr un servicio fuera de Docker (apunta a localhost para Cassandra/RabbitMQ vía appsettings.Development.json)
dotnet run --project services/teams/Esports.Teams.Api

# Seed de datos de ejemplo (Fase 6)
docker compose run --rm seeder
```

URLs una vez levantado: gateway `http://localhost:8080`, Swagger por servicio `:5001`–`:5004/swagger`, RabbitMQ management `http://localhost:15672`.

## Notas cross-platform (Mac/Windows)

- `.gitattributes` fuerza LF en `*.sh`, `*.cs`, `*.csproj`, `*.json`, `*.yml`, `Dockerfile`, etc. — crítico para que los contenedores Linux no fallen con CRLF de Windows.
- Imágenes multi-arch (`cassandra:5.0`, `rabbitmq:3-management`, `dotnet/sdk:10.0`/`aspnet:10.0`) — sin emulación en Apple Silicon.
- `DOTNET_USE_POLLING_FILE_WATCHER=1` necesario para hot reload confiable en bind mounts (especialmente Windows).
