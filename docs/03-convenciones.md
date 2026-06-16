# 03 — Convenciones

> Reglas para que los 3 integrantes + los agentes escriban código consistente. Si dudas de un nombre, está acá.

## Casing por contexto (esto resuelve el "sin mayúscula acá")

El casing **depende de dónde** estás escribiendo. No es uniforme:

| Contexto | Estilo | Ejemplo |
|---|---|---|
| Namespaces, clases, métodos, propiedades públicas (C#) | `PascalCase` | `TorneoRepository`, `GetByIdAsync` |
| Campos privados (C#) | `_camelCase` | `_session`, `_httpClient` |
| Variables locales / parámetros (C#) | `camelCase` | `torneoId`, `nombreEquipo` |
| Keyspaces, tablas, columnas (Cassandra/CQL) | `snake_case` minúscula | `esports_teams`, `torneos_por_equipo`, `nombre_equipo` |
| Servicios Docker, imágenes, carpetas del repo | `kebab-case` / minúscula | `tournaments`, `esports-cassandra` |
| Rutas REST | minúscula, `kebab-case` | `/api/torneos`, `/api/ranking/equipos` |
| Tipos de evento (MassTransit) | `PascalCase` | `TeamRegisteredToTournament`, `MatchPlayed` |
| Variables de entorno | `PascalCase__Anidado` (doble guion bajo = sección) | `Cassandra__ContactPoints` |

**Regla mental:** Cassandra y Docker = minúsculas con guiones; C# = PascalCase (o `_camelCase` para privados).

## Idioma

- **Dominio en español**: entidades, tablas, columnas y rutas usan los términos del negocio en español (`Torneo`, `Equipo`, `Jugador`, `Partida`, `Premio`, `Videojuego`, `Organizador`, `torneos_por_equipo`). Mantiene coherencia con el modelo Chebotko ya entregado y con el equipo.
- **Técnico/infraestructura en inglés**: los sufijos y piezas técnicas van en inglés (`Repository`, `Service`, `Controller`, `Dto`, `Consumer`, `Publisher`), igual que los namespaces (`Esports.Tournaments.Api`).
- Ejemplo de clase: `public class TorneoRepository` (dominio español + sufijo técnico inglés). Entidad: `public class Torneo`.

## Namespaces

Raíz: `Esports`. Patrón por servicio: `Esports.<Servicio>.Api` y subnamespaces por carpeta.

```
Esports.Teams.Api          (.Controllers, .Domain, .Repositories, .Services, .Dtos, .Cassandra)
Esports.Tournaments.Api    (..., .Events)          # publica eventos
Esports.Matches.Api        (..., .Events)          # publica eventos
Esports.Ranking.Api        (..., .Consumers)       # consume eventos

Esports.Gateway
Esports.Shared             // contratos de eventos + DTOs compartidos
Esports.Shared.Events      // los records de eventos (TeamRegisteredToTournament, MatchPlayed)
```

## Estructura de carpetas por servicio

Un solo proyecto por servicio, organizado por carpetas (no Clean Architecture multi-proyecto):

```
services/teams/Esports.Teams.Api/
├── Controllers/        # endpoints HTTP (delgados: validan y delegan)
│   ├── JugadoresController.cs
│   └── EquiposController.cs
├── Domain/             # entidades del dominio
│   ├── Jugador.cs
│   └── Equipo.cs
├── Dtos/               # request/response (lo que ve el cliente)
├── Repositories/       # acceso a Cassandra (todo el CQL vive acá)
│   ├── IJugadorRepository.cs / JugadorRepository.cs
│   └── IEquipoRepository.cs / EquipoRepository.cs
├── Services/           # lógica de negocio (orquesta repos, REST, eventos)
├── Cassandra/          # sesión + bootstrap de keyspace/tablas
│   ├── CassandraSession.cs
│   └── SchemaInitializer.cs
├── Program.cs          # composición (DI, middlewares, swagger)
├── appsettings.json
├── Esports.Teams.Api.csproj
└── Dockerfile
```

Por servicio:
- `tournaments` agrega `Events/` (publisher de `TeamRegisteredToTournament`). Es el más grande: dominios de videojuegos, organizadores, torneos, inscripciones y premios — agrupar los controllers/repos por sub-dominio (`VideojuegosController`, `OrganizadoresController`, `TorneosController`, `InscripcionesController`, `PremiosController`).
- `matches` agrega `Events/` (publisher de `MatchPlayed`).
- `ranking` agrega `Consumers/` (consume ambos eventos). Sus "repositories" escriben los counters y leen los read-models.

## Convenciones de código

- **Todo async**: métodos de repos y servicios terminan en `Async` y devuelven `Task`/`Task<T>`. Nada de `.Result` ni `.Wait()`.
- **Controllers delgados**: validan input y llaman al `Service`. La lógica no vive en el controller.
- **Repositories**: única capa que habla CQL. Preparan statements (`PreparedStatement`) una vez y los reusan. Mapean filas (`Row`) a entidades/DTOs. Los dual-writes usan `BATCH`.
- **DTOs separados de Domain**: el cliente nunca ve la entidad cruda; siempre un DTO de response.
- **Ids**: `uuid` en Cassandra → `Guid` en C#. Los genera el servidor (`Guid.NewGuid()`), salvo que el cliente provea uno.
- **Fechas**: `timestamp` → `DateTimeOffset`/`DateTime` UTC; `date` (Cassandra) → `LocalDate` del driver. Guardar siempre en UTC. Para Q18, derivar `dia` a partir del `fecha` (timestamp) de la partida.
- **Counters**: las tablas de ranking/stats se escriben SOLO con `UPDATE ... SET col = col + 1`. Nunca `INSERT`. La fila se crea sola en el primer incremento.
- **Errores**: `ProblemDetails` (RFC 7807). 404 si no existe, 400 si el input es inválido, 502/503 si un servicio dependiente (REST) falla. Nunca stack traces al cliente.
- **Logging**: `ILogger<T>`. Loguear escrituras y errores. Nada de `Console.WriteLine`.

## Configuración

Cada servicio lee config de `appsettings.json` + variables de entorno (las de entorno ganan; las inyecta `docker-compose.yml`). Secciones estándar:

```json
{
  "Cassandra": { "ContactPoints": "localhost", "Port": 9042, "Keyspace": "esports_teams" },
  "RabbitMq":  { "Host": "localhost", "Username": "guest", "Password": "guest" },
  "Services":  { "Teams": "http://localhost:5001", "Tournaments": "http://localhost:5002" }
}
```

En Docker se sobreescriben con `Cassandra__ContactPoints=cassandra`, `RabbitMq__Host=rabbitmq`, `Services__Teams=http://teams:8080`, etc. (nombres de servicio de la red de Docker, no `localhost`).

## Git

- **Una rama por servicio/feature**: `feat/teams`, `feat/tournaments`, `feat/matches`, `feat/ranking`, `feat/gateway`. Merge a `main` cuando el servicio levanta y pasa su smoke test.
- **Commits en imperativo, cortos**: `feat(teams): jugador por nickname (Q1)`, `fix(ranking): counters en Q7/Q22/Q23`.
- **`.gitattributes` obligatorio** (ver `docs/06`): fuerza LF para que scripts y configs funcionen igual en Windows y Mac dentro de los contenedores Linux.
- **`.gitignore`**: `bin/`, `obj/`, `.vs/`, `*.user`, archivos de entorno locales.
