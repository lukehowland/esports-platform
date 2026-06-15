# 03 — Convenciones

> Reglas para que los 3 integrantes + los agentes escriban código consistente. Si dudas de un nombre, está acá.

## Casing por contexto (esto resuelve el "sin mayúscula acá")

El casing **depende de dónde** estás escribiendo. No es uniforme:

| Contexto | Estilo | Ejemplo |
|---|---|---|
| Namespaces, clases, métodos, propiedades públicas (C#) | `PascalCase` | `TournamentRepository`, `GetByIdAsync` |
| Campos privados (C#) | `_camelCase` | `_session`, `_httpClient` |
| Variables locales / parámetros (C#) | `camelCase` | `torneoId`, `nombreEquipo` |
| Keyspaces, tablas, columnas (Cassandra/CQL) | `snake_case` minúscula | `esports_teams`, `torneos_por_equipo`, `nombre_equipo` |
| Servicios Docker, imágenes, carpetas del repo | `kebab-case` / minúscula | `tournaments`, `esports-cassandra` |
| Rutas REST | minúscula, `kebab-case` | `/api/torneos`, `/api/ranking/global` |
| Tipos de evento (MassTransit) | `PascalCase` | `TeamRegisteredToTournament` |
| Variables de entorno | `PascalCase__Anidado` (doble guion bajo = sección) | `Cassandra__ContactPoints` |

**Regla mental:** Cassandra y Docker = minúsculas con guiones; C# = PascalCase (o `_camelCase` para privados).

## Idioma

- **Dominio en español**: las entidades, tablas, columnas y rutas usan los términos del negocio en español (`Torneo`, `Equipo`, `Jugador`, `Partida`, `Premio`, `torneos_por_equipo`). Esto mantiene coherencia con el modelo Chebotko ya entregado y con el equipo.
- **Técnico/infraestructura en inglés**: los sufijos y piezas técnicas van en inglés (`Repository`, `Service`, `Controller`, `Dto`, `Consumer`, `Publisher`), igual que los namespaces (`Esports.Tournaments.Api`).
- Ejemplo de clase: `public class TorneoRepository` (dominio español + sufijo técnico inglés). Entidad: `public class Torneo`.

## Namespaces

Raíz: `Esports`. Patrón por servicio: `Esports.<Servicio>.Api` y subnamespaces por carpeta.

```
Esports.Teams.Api
Esports.Teams.Api.Controllers
Esports.Teams.Api.Domain
Esports.Teams.Api.Repositories
Esports.Teams.Api.Services
Esports.Teams.Api.Dtos
Esports.Teams.Api.Cassandra

Esports.Tournaments.Api (.Controllers, .Domain, .Repositories, .Services, .Dtos, .Cassandra, .Events)
Esports.Matches.Api (...)
Esports.Ranking.Api (..., .Consumers)

Esports.Gateway
Esports.Shared            // contratos de eventos + DTOs compartidos
Esports.Shared.Events     // los records de eventos
```

## Estructura de carpetas por servicio

Un solo proyecto por servicio, organizado por carpetas (no Clean Architecture multi-proyecto):

```
services/teams/Esports.Teams.Api/
├── Controllers/        # endpoints HTTP (delgados: validan y delegan)
│   ├── EquiposController.cs
│   └── JugadoresController.cs
├── Domain/             # entidades del dominio
│   ├── Equipo.cs
│   └── Jugador.cs
├── Dtos/               # request/response (lo que ve el cliente)
│   ├── CrearEquipoRequest.cs
│   └── EquipoResponse.cs
├── Repositories/       # acceso a Cassandra (todo el CQL vive acá)
│   ├── IEquipoRepository.cs
│   └── EquipoRepository.cs
├── Services/           # lógica de negocio (orquesta repos, REST, eventos)
│   └── EquipoService.cs
├── Cassandra/          # sesión + bootstrap de keyspace/tablas
│   ├── CassandraSession.cs
│   └── SchemaInitializer.cs
├── Program.cs          # composición (DI, middlewares, swagger)
├── appsettings.json
├── appsettings.Development.json
├── Esports.Teams.Api.csproj
└── Dockerfile
```

`tournaments` agrega `Events/` (publisher). `ranking` agrega `Consumers/`. `matches` igual que `teams`.

## Convenciones de código

- **Todo async**: métodos de repos y servicios terminan en `Async` y devuelven `Task`/`Task<T>`. Nada de `.Result` ni `.Wait()`.
- **Controllers delgados**: validan input y llaman al `Service`. La lógica no vive en el controller.
- **Repositories**: única capa que habla CQL. Preparan statements (`PreparedStatement`) una vez y los reusan. Mapean filas (`Row`) a entidades/DTOs.
- **DTOs separados de Domain**: el cliente nunca ve la entidad cruda; siempre un DTO de response.
- **Ids**: `uuid` en Cassandra → `Guid` en C#. Los genera el servidor (`Guid.NewGuid()`), no el cliente, salvo que el cliente lo provea explícitamente.
- **Fechas**: `timestamp` en Cassandra → `DateTimeOffset`/`DateTime` UTC en C#. Guardar siempre en UTC.
- **Errores**: usar `ProblemDetails` (RFC 7807). 404 si no existe, 400 si el input es inválido, 502/503 si un servicio dependiente (REST) falla. Nunca devolver stack traces al cliente.
- **Validación**: validar en el borde (controller/DTO). Si `equipoId` no existe al inscribir, devolver 400/404 con mensaje claro.
- **Logging**: `ILogger<T>`. Loguear inicio/fin de operaciones de escritura y errores. Nada de `Console.WriteLine`.

## Configuración

Cada servicio lee config de `appsettings.json` + variables de entorno (las de entorno ganan; las inyecta `docker-compose.yml`). Secciones estándar:

```json
{
  "Cassandra": {
    "ContactPoints": "localhost",
    "Port": 9042,
    "Keyspace": "esports_teams"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Services": {
    "Teams": "http://localhost:5002",
    "Tournaments": "http://localhost:5001"
  }
}
```

En Docker estos valores se sobreescriben con `Cassandra__ContactPoints=cassandra`, `RabbitMq__Host=rabbitmq`, `Services__Teams=http://teams:8080`, etc. (nombres de servicio de la red de Docker, no `localhost`).

## Git

- **Una rama por servicio/feature**: `feat/teams`, `feat/tournaments`, `feat/gateway`. Merge a `main` cuando el servicio levanta y pasa su smoke test.
- **Commits atómicos, en inglés, formato Conventional Commits**: ver `docs/08-commits.md` para el detalle completo (tipos, scopes, ejemplos). Ej: `feat(teams): add players by country endpoint (Q3)`, `fix(ranking): increment total_torneos via counter`.
- **`.gitattributes` obligatorio** (ver `docs/06`): fuerza LF para que los scripts y configs funcionen igual en Windows y Mac dentro de los contenedores Linux.
- **`.gitignore`**: `bin/`, `obj/`, `.vs/`, `*.user`, archivos de entorno locales.
