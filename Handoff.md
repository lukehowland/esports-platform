# Handoff - Esports Platform auth-service

Fecha: 2026-06-16
Repo: `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`  
Rama de trabajo: `feat/auth-service`
Motivo: este archivo deja el contexto listo para que CloudCode/Claude continue si esta sesion se queda sin limite.

Este handoff reemplaza el handoff viejo. El anterior mezclaba estado de `main`, commits locales antiguos y una seccion auth pegada al final. Esta version es la fuente actual para retomar la rama.

## Estado ejecutivo

La plataforma ya tenia los fixes de infraestructura/datos/frontend manual hechos antes de la rama auth:

- `docker compose down` elimina el entorno demo porque Cassandra no usa volumen persistente en Compose.
- `docker compose up --build -d` levanta Cassandra, RabbitMQ, servicios, gateway, seeder y frontend.
- El seeder corre automaticamente, autentica como admin, carga dataset rico e idempotente, registra usuarios demo por rol y termina `Exited (0)`.
- El manual del frontend funciona en Docker porque `MANUAL-USUARIO.md` se copia a la imagen.
- El backend rechaza entradas vacias o basura antes de escribir en Cassandra.

Sobre eso, esta rama agrega el microservicio `auth` y RBAC real en backend. Ya no dependemos de roles simulados solo en `localStorage`.

## Estado actual verificado

Ultima verificacion hecha antes de este handoff:

```bash
docker compose config --quiet
git diff --check
docker compose build tests
docker compose down --remove-orphans
docker compose up --build -d
docker compose run --rm --no-deps tests
docker compose down --remove-orphans
docker compose up --build -d
docker compose ps --all
docker compose logs --tail=35 seeder
```

Resultados:

- Compose valido.
- Sin whitespace errors en `git diff --check`.
- Arranque limpio desde cero completado.
- Todos los contenedores quedan healthy: `auth`, `teams`, `tournaments`, `matches`, `ranking`, `gateway`, `frontend`, `cassandra`, `rabbitmq`.
- `seeder` queda `Exited (0)`.
- Tests de integracion: `122/122` pasan.
- Despues de correr tests se hizo otro `down`/`up --build -d`, asi que el stack actual quedo limpio, solo con datos del seeder.

Estado Docker final observado:

```text
auth          healthy  5005
teams         healthy  5001
tournaments   healthy  5002
matches       healthy  5003
ranking       healthy  5004
gateway       healthy  8080
frontend      healthy  3000
cassandra     healthy  9042
rabbitmq      healthy  5672 / 15672
seeder        Exited (0)
```

## Branch y commits base

Rama actual:

```bash
git status --short --branch
# ## feat/auth-service
```

Antes de los commits de auth, el log local tenia:

```text
e0b1349 docs: update handoff and automatic seed guidance
c2321d5 fix(api): reject invalid demo data inputs
817b4fa fix(frontend): include user manual in Docker image
0582107 feat(seed): load rich idempotent demo dataset
ae5302c build(infra): run seeder after clean compose startup
8e58d4d origin/main build(infra): add frontend service to compose
```

Importante: `origin/main` estaba en `8e58d4d`. La rama `feat/auth-service` contiene tambien los commits locales de infraestructura/seeder/manual/validaciones hechos antes de auth. Si se crea PR contra `main`, el PR incluira esos commits y los de auth, salvo que antes se actualice `origin/main`.

## Plan original de auth

El plan que ejecuto el agente anterior esta en:

```bash
cat ~/.claude/plans/peaceful-hatching-hoare.md
sed -n '1,240p' ~/.claude/plans/peaceful-hatching-hoare.md
```

Resumen del plan:

- Agregar `shared/Esports.Auth.Shared`.
- Agregar microservicio `services/auth/Esports.Auth.Api`.
- Agregar ruta `/api/auth/**` al gateway.
- Proteger mutaciones de `teams`, `tournaments` y `matches`.
- Adaptar seeder para autenticarse como admin y registrar usuarios demo.
- Adaptar tests y documentacion.
- No tocar frontend en esta tanda.

## Arquitectura actual

Servicios:

| Servicio | Keyspace | Puerto host | Responsabilidad |
|---|---|---:|---|
| `auth` | `esports_auth` | 5005 | login, registro admin-only, JWT, roles |
| `teams` | `esports_teams` | 5001 | equipos y jugadores |
| `tournaments` | `esports_tournaments` | 5002 | videojuegos, organizadores, torneos, inscripciones, premios |
| `matches` | `esports_matches` | 5003 | partidas y enfrentamientos |
| `ranking` | `esports_ranking` | 5004 | read models por eventos |
| `gateway` | n/a | 8080 | YARP, entrada unica |
| `frontend` | n/a | 3000 | Next.js |

Reglas duras que siguen vigentes:

- Un keyspace por servicio.
- No cross-keyspace.
- Lecturas entre servicios por `HttpClient` tipado.
- Eventos por MassTransit/RabbitMQ.
- Ranking usa counters y solo se escribe por eventos.
- Mutaciones multi-tabla dentro del mismo servicio usan `BATCH`.
- Frontend debe consumir el gateway, no servicios directos.

## Auth implementado

Nuevo proyecto compartido:

- `shared/Esports.Auth.Shared/AuthConstants.cs`
- `shared/Esports.Auth.Shared/ClaimsPrincipalExtensions.cs`
- `shared/Esports.Auth.Shared/JwtAuthExtensions.cs`

Nuevo microservicio:

- `services/auth/Esports.Auth.Api`
- Keyspace: `esports_auth`
- Tabla: `usuarios`
- Admin demo sembrado por `SchemaInitializer`.
- Passwords con PBKDF2.
- JWT HS256 con `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`.

Endpoints:

- `POST /api/auth/login`
- `POST /api/auth/register`
- `GET /api/auth/me`

`/api/auth/register` esta protegido con rol `admin` y ahora valida combinaciones de rol:

- `admin`: sin `organizadorId` ni `equipoId`.
- `fan`: sin `organizadorId` ni `equipoId`.
- `organizador`: requiere `organizadorId` y no puede tener `equipoId`.
- `capitan`: requiere `equipoId` y no puede tener `organizadorId`.
- Roles invalidos devuelven `400 BadRequest`.

Correcciones hechas durante auditoria:

- `UsuarioRepository` usaba `ISession` ambiguo; se corrigio con `global::Cassandra.ISession`.
- Dockerfiles de `teams`, `tournaments` y `matches` no copiaban `shared/Esports.Auth.Shared/*.csproj` antes de restore.
- `/api/auth/me` devolvia username vacio; se agrego claim `username` y `NameClaimType`.
- Se endurecio `register` para impedir identidades RBAC mal formadas.

## RBAC actual

Lecturas `GET` Q1-Q24 quedan publicas.

Mutaciones protegidas:

| Endpoint | Regla |
|---|---|
| `POST /api/equipos` | solo `admin` |
| `POST /api/equipos/{equipoId}/jugadores` | `admin` o `capitan` con `equipo_id == equipoId` |
| `POST /api/videojuegos` | `admin` u `organizador` |
| `POST /api/organizadores` | solo `admin` |
| `POST /api/torneos` | `admin` u `organizador` con `organizador_id == organizadorId` |
| `POST /api/torneos/{torneoId}/inscripciones` | `admin` o `capitan` con `equipo_id == equipoId` |
| `POST /api/torneos/{torneoId}/premios` | `admin` u `organizador` dueno del torneo |
| `POST /api/partidas` | `admin` u `organizador` dueno del torneo |
| `POST /api/auth/register` | solo `admin` y rol/vinculos validos |

Para `POST /api/partidas`, `matches` no lee otro keyspace. Usa `TournamentsClient` tipado contra `tournaments` para verificar el `organizadorId` del torneo.

Respuesta esperada:

- Sin token: `401`.
- Token valido pero rol/ownership incorrecto: `403`.
- Registro de usuario con rol/vinculos incoherentes: `400`.

## Tests RBAC

Archivo principal:

- `tests/Esports.Gateway.Tests/AuthTests.cs`

Cobertura de auth/RBAC agregada:

- Login admin y `/api/auth/me` con `username`.
- Usuarios demo tienen rol y vinculo correctos:
  - `org_riot` -> `organizador` + `RIOTId`
  - `cap_t1` -> `capitan` + `T1Id`
  - `fan_demo` -> `fan`
- Lecturas publicas siguen funcionando sin token.
- Mutacion sin token devuelve `401`.
- Fan no puede registrar usuario ni crear videojuego.
- Admin puede registrar fan.
- Admin no puede registrar rol invalido.
- Admin no puede registrar organizador sin `organizadorId`.
- Admin no puede registrar capitan sin `equipoId`.
- Admin no puede registrar fan con vinculo a equipo.
- Organizador puede crear videojuego.
- Organizador no puede crear otro organizador.
- Organizador no puede crear torneo para otro organizador.
- Organizador puede crear torneo propio.
- Capitan no puede crear equipo.
- Capitan puede agregar jugador a su equipo.
- Capitan no puede agregar jugador a equipo ajeno.
- Capitan puede inscribir su equipo.
- Capitan no puede inscribir equipo ajeno.
- Organizador puede asignar premio en torneo propio.
- Organizador no puede asignar premio en torneo ajeno.
- Organizador puede registrar partida de torneo propio.
- Organizador no puede registrar partida de torneo ajeno.

La suite completa paso:

```text
Total tests: 122
Passed: 122
```

Warnings no bloqueantes observados:

- xUnit analyzer: algunos `Assert.True` deberian ser `Assert.Contains`.
- `Newtonsoft.Json` 9.0.1 aparece con advisory NU1903 por dependencia transitiva.
- `Rfc2898DeriveBytes` constructor usado en `PasswordService` marca SYSLIB0060; conviene migrar luego a `Rfc2898DeriveBytes.Pbkdf2`.

## Seeder y usuarios demo

El seeder:

- Hace login admin en `/api/auth/login`.
- Envia `Authorization: Bearer <token>` en todos los POSTs.
- Puebla datos conectados y ranking por eventos.
- Registra usuarios demo por rol al final.

Credenciales demo:

```text
admin / admin-dev-password
org_riot / OrgDemo2024
org_esl / OrgDemo2024
org_vct / OrgDemo2024
cap_t1 / CapDemo2024
cap_navi / CapDemo2024
cap_g2 / CapDemo2024
fan_demo / FanDemo2024
```

Hay mas usuarios `org_<code>` y `cap_<tag>` creados por el seeder. Ver logs:

```bash
docker compose logs seeder
```

Resumen del seed observado:

```text
Equipos: 40
Torneos: 12
Organizadores: 7
Ranking equipos por torneos: 40
Ranking equipos por victorias: 40
Ranking jugadores activos: 50
```

## Archivos principales tocados

Auth/shared:

- `shared/Esports.Auth.Shared/*`
- `services/auth/Esports.Auth.Api/*`
- `Esports.sln`

Servicios protegidos:

- `services/teams/Esports.Teams.Api/Controllers/EquiposController.cs`
- `services/teams/Esports.Teams.Api/Program.cs`
- `services/teams/Esports.Teams.Api/Esports.Teams.Api.csproj`
- `services/teams/Esports.Teams.Api/Dockerfile`
- `services/tournaments/Esports.Tournaments.Api/Controllers/*.cs`
- `services/tournaments/Esports.Tournaments.Api/Program.cs`
- `services/tournaments/Esports.Tournaments.Api/Esports.Tournaments.Api.csproj`
- `services/tournaments/Esports.Tournaments.Api/Dockerfile`
- `services/matches/Esports.Matches.Api/Controllers/PartidasController.cs`
- `services/matches/Esports.Matches.Api/Clients/TournamentsClient.cs`
- `services/matches/Esports.Matches.Api/Program.cs`
- `services/matches/Esports.Matches.Api/Esports.Matches.Api.csproj`
- `services/matches/Esports.Matches.Api/Dockerfile`

Infra/seeder/tests/docs:

- `docker-compose.yml`
- `gateway/Esports.Gateway/appsettings.json`
- `tools/Esports.Seeder/Program.cs`
- `tests/Esports.Gateway.Tests/*`
- `AGENTS.md`
- `CLAUDE.md`
- `README.MD`
- `USER-STORIES.md`
- `docs/01-arquitectura.md`
- `docs/03-convenciones.md`
- `docs/04-contratos-api.md`
- `docs/06-docker-setup.md`
- `docs/07-plan-ejecucion.md`
- `Handoff.md`

## Commits recomendados para cerrar esta rama

Mantener Conventional Commits en ingles segun `docs/08-commits.md`.

Orden recomendado:

```text
feat(auth): add JWT auth service
feat(auth): enforce role ownership on mutations
build(infra): wire auth through compose and gateway
fix(seed): authenticate demo data writes
test(auth): cover role ownership rules
docs(auth): document auth service handoff
```

Notas:

- El commit docs debe incluir este `Handoff.md`.
- Los commits anteriores de datos/infra ya existen en la rama antes de auth.
- Si se crea PR ahora contra `main`, incluira tambien esos commits locales no presentes en `origin/main`.

## Pull request

Pendiente en el momento de escribir este handoff:

```bash
git push -u origin feat/auth-service
gh pr create --base main --head feat/auth-service
```

Usar titulo sugerido:

```text
feat(auth): add JWT auth service and backend RBAC
```

Cuerpo sugerido:

```text
## Summary
- Add auth microservice with JWT login/register/me and demo admin seed.
- Protect backend mutations with role and ownership checks across teams, tournaments, and matches.
- Authenticate the seeder and register demo users by role.
- Update docs and handoff for CloudCode continuation.

## Verification
- docker compose config --quiet
- git diff --check
- docker compose down --remove-orphans
- docker compose up --build -d
- docker compose run --rm --no-deps tests

## Test result
- 122/122 integration tests passing.
```

## Siguiente fase: frontend con auth real

No se toco frontend en esta tanda por decision de alcance. Siguiente agente debe:

- Reemplazar el selector demo/localStorage por login real contra `/api/auth/login`.
- Guardar token de forma razonable para demo.
- Llamar `/api/auth/me` al cargar sesion.
- Adjuntar `Authorization: Bearer <token>` en mutaciones.
- Renderizar home/dashboard distinto por rol:
  - `admin`: backoffice y carga/setup.
  - `organizador`: crear videojuegos, torneos, premios y partidas solo propias.
  - `capitan`: gestionar su equipo e inscribirlo.
  - `fan`: lectura/rankings/manual/torneos.
- Ocultar acciones no permitidas por rol, pero recordar que la seguridad real esta en backend.
- Arreglar problemas detectados previamente:
  - selector de rol no cambiaba la experiencia real.
  - login era solo frontend.
  - pagina de jugadores no mostraba datos.
  - ranking mostraba IDs donde deberia resolver nombres.
  - revisar manual en UI.

## Como retomar rapido

Comandos:

```bash
cd /Users/lukesito/dev/src/github.com/lukehowland/esports-platform
git status --short --branch
git log --oneline --decorate -n 12
docker compose ps --all
docker compose logs --tail=80 seeder
docker compose run --rm --no-deps tests
```

Si hay duda sobre el plan original:

```bash
cat ~/.claude/plans/peaceful-hatching-hoare.md
```

Si se quiere refrescar desde cero:

```bash
docker compose down --remove-orphans
docker compose up --build -d
docker compose ps --all
```

Estado deseado al retomar:

- Rama `feat/auth-service`.
- Stack arriba y healthy.
- Seeder `Exited (0)`.
- Backend auth/RBAC listo y testeado.
- Trabajo pendiente principal: frontend conectado a auth real y PR review/merge.
