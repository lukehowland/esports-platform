# Handoff completo - Esports Platform

Fecha de handoff: 2026-06-16  
Repo: `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`  
Rama actual: `main`  
Remote: `origin git@github.com:lukehowland/esports-platform.git`  
HEAD de codigo antes del commit final de documentacion: `c2321d5 fix(api): reject invalid demo data inputs`  
Estado remoto: `origin/main` sigue en `8e58d4d build(infra): add frontend service to compose` hasta que se haga push.  
Estado de trabajo antes del commit final de documentacion: quedaban pendientes `AGENTS.md`, `CLAUDE.md`, `README.MD`, `docs/06-docker-setup.md`, `docs/07-plan-ejecucion.md` y este `Handoff.md`.

Este documento existe para que otro agente pueda continuar el proyecto sin perder contexto de esta conversacion. No es solo una auditoria frontend; incluye arquitectura, commits hechos, decisiones, fixes de Docker/seeder/datos, validaciones, hallazgos de frontend, preocupaciones del usuario y siguiente plan.

---

## 1. Contexto del producto

Estamos construyendo una plataforma de gestion de torneos de eSports para Sistemas Distribuidos, UNIVALLE.

El backend es una arquitectura de microservicios .NET 10 sobre Apache Cassandra 5.0, modelada query-first con metodologia Chebotko. El sistema cubre 24 queries (Q1-Q24) repartidas en 24 tablas desnormalizadas, mas tablas base auxiliares por id para lookup.

Servicios:

| Servicio | Keyspace | Host dev | Dominio | Queries |
|---|---|---:|---|---|
| `teams` | `esports_teams` | 5001 | jugadores y equipos | Q1-Q6 |
| `tournaments` | `esports_tournaments` | 5002 | videojuegos, organizadores, torneos, inscripciones, premios | Q8-Q15, Q20, Q21 |
| `matches` | `esports_matches` | 5003 | partidas/enfrentamientos | Q16-Q19 |
| `ranking` | `esports_ranking` | 5004 | rankings y estadisticas derivadas por eventos | Q7, Q22, Q23, Q24 |
| `gateway` | n/a | 8080 | YARP, entrada unica para el frontend | n/a |
| `frontend` | n/a | 3000 | Next.js web app | consume gateway |

Regla central: el frontend solo debe hablar con `http://localhost:8080` via el gateway o con su proxy interno `/api/...` que apunta al gateway.

---

## 2. Reglas duras del repo

Leer y respetar `AGENTS.md`, `CLAUDE.md` y `docs/`.

Restricciones importantes:

- .NET 10, Cassandra 5.0, RabbitMQ 3-management.
- Driver Cassandra: `CassandraCSharpDriver`.
- No usar Entity Framework.
- MassTransit debe ser 8.x; no instalar MassTransit 9.x.
- Gateway con YARP.
- Swagger en cada servicio.
- Config por variables de entorno: `Cassandra__ContactPoints`, `Cassandra__Keyspace`, `RabbitMq__Host`, `Services__<Nombre>`.
- Una base/keyspace por servicio. Ningun servicio lee ni escribe el keyspace de otro.
- Si se necesitan datos ajenos: REST sincronico via `HttpClient` tipado o eventos asincronicos con MassTransit.
- Mutaciones que escriben varias tablas desnormalizadas dentro de un servicio deben ir en `BATCH`.
- Ranking/stats son read-models derivados. Solo `ranking` los escribe consumiendo eventos.
- Tablas counter de ranking: solo `UPDATE`, nunca `INSERT`.
- Cassandra: primary key inmutable; no updates sobre partition/clustering keys.
- C# PascalCase; Cassandra snake_case; rutas/carpetas Docker lower/kebab.
- Errores con `ProblemDetails`.
- Commits en ingles y Conventional Commits. Ver `docs/08-commits.md`.

---

## 3. Documentacion importante ya leida

Se leyo y sintetizo el proyecto:

- `AGENTS.md`
- `CLAUDE.md`
- `README.MD`
- `USER-STORIES.md`
- `docs/01-arquitectura.md`
- `docs/02-modelo-datos.md`
- `docs/03-convenciones.md`
- `docs/04-contratos-api.md`
- `docs/05-eventos.md`
- `docs/06-docker-setup.md`
- `docs/07-plan-ejecucion.md`
- `docs/08-commits.md`
- `frontend/MANUAL-USUARIO.md`
- Codigo backend, frontend, seeder, gateway y tests.

Tambien se guardo una memoria previa de contexto en:

- `/Users/lukesito/.codex/memories/extensions/ad_hoc/notes/2026-06-16T09-11-25-0400-esports-platform-context.md`

Esa memoria fue una sintesis inicial del proyecto antes de los cambios recientes de Docker/seeder/frontend audit.

---

## 4. Historial de commits ya hechos y pusheados

Estos commits estan en `main` y ya estan en `origin/main`:

1. `1b2087f chore: add repo scaffolding (gitattributes, gitignore, readme placeholder)`
   - Agrego `.gitattributes`, `.gitignore`, `README.MD` placeholder.

2. `53e78ba docs: add architecture, data model, conventions, and execution plan`
   - Agrego docs iniciales `01` a `07`.

3. `5d00e12 docs: add agent guide and commit conventions`
   - Agrego `CLAUDE.md` y `docs/08-commits.md`.

4. `eff26c3 chore: phase 0 scaffolding - solution, shared events, compose infra`
   - Agrego `Esports.sln`, eventos compartidos, compose inicial, docs ajustadas.

5. `e5d40f6 feat(teams): implement teams service - Q1-Q6 end-to-end (phase 1 - gold template)`
   - Implemento servicio `teams`, schema Cassandra, controllers, repos, services y Dockerfile.

6. `aa2ce7f feat: implement tournaments, matches, ranking, gateway, and seeder - Q7-Q24 end-to-end`
   - Implemento `tournaments`, `matches`, `ranking`, `gateway`, eventos, consumidores y seeder inicial.

7. `0222686 test: add 97 integration tests for all Q1-Q24 endpoints via gateway`
   - Agrego tests de gateway para teams, tournaments, matches, ranking y errores.

8. `40f95ee chore(infra): ignore frontend dependency artifacts`
   - Ajusto `.gitignore` para no trackear dependencias/artifacts frontend.

9. `9ba5194 docs: add Codex agent project instructions`
   - Agrego `AGENTS.md`.

10. `73ca039 feat(frontend): add Next.js web application`
    - Agrego app Next.js completa: rutas, UI, proxy `/api`, login demo, manual, pantallas Q1-Q24.

11. `8e58d4d build(infra): add frontend service to compose`
    - Conecto frontend al `docker-compose.yml`.

Despues de `8e58d4d` se hicieron estos commits locales nuevos durante el cierre de esta conversacion:

12. `ae5302c build(infra): run seeder after clean compose startup`
    - Quita persistencia de Cassandra en el entorno demo.
    - Hace que `seeder` corra dentro del flujo normal de Compose.
    - Hace que `frontend` y `tests` esperen a que el seeder termine correctamente.

13. `0582107 feat(seed): load rich idempotent demo dataset`
    - Rehace el seeder para que sea idempotente y robusto.
    - Agrega dataset rico conectado para videojuegos, organizadores, equipos, jugadores, torneos, inscripciones, premios, partidas y rankings.
    - El seeder valida respuestas y falla con status/body si un POST falla.

14. `817b4fa fix(frontend): include user manual in Docker image`
    - Permite que `/manual` funcione dentro de Docker.
    - Ajusta `frontend/.dockerignore`, `frontend/Dockerfile` y `frontend/MANUAL-USUARIO.md`.

15. `c2321d5 fix(api): reject invalid demo data inputs`
    - Agrega validaciones backend en teams, tournaments y matches.
    - Normaliza datos para evitar entradas vacias o basura en Cassandra.
    - Ajusta test de error para esperar `400 BadRequest` en equipo con nombre vacio.

Quedaba pendiente un commit final de documentacion/handoff con `AGENTS.md`, `CLAUDE.md`, `README.MD`, `docs/06-docker-setup.md`, `docs/07-plan-ejecucion.md` y este archivo.

---

## 5. Estado git actual

`git status --short --branch` antes del commit final de documentacion:

```text
## main
 M AGENTS.md
 M CLAUDE.md
 M README.MD
 M docs/06-docker-setup.md
 M docs/07-plan-ejecucion.md
?? Handoff.md
```

`git diff --stat` antes de los commits locales mostraba cambios fuertes en:

- `tools/Esports.Seeder/Program.cs` con rewrite importante.
- `docker-compose.yml`.
- docs/setup/manual.
- validaciones backend en `teams`, `tournaments`, `matches`.
- fix Docker del manual frontend.

`Handoff.md` queda incluido en el commit final de documentacion/handoff.

---

## 6. Cambio solicitado inicialmente en esta conversacion

El usuario primero pidio:

- Ver estado de git.
- Resolver explosion de lineas adicionales y archivos que no debian trackearse.
- Ajustar `.gitignore`.

Resultado:

- Se identificaron artifacts/dependencias frontend generando muchas lineas.
- Se agrego commit `40f95ee chore(infra): ignore frontend dependency artifacts`.
- Se mantuvo el repo limpio de `frontend/node_modules`, stores y artifacts.

Luego el usuario pidio:

- Leer todo el proyecto.
- Entender docs, cloud/Claude, historias de usuario y manual.
- Guardar memoria del contexto.
- Leer documentacion de commits.
- Guardar estado actual con commits atomicos.
- Hacer push.

Resultado:

- Se leyo y sintetizo el proyecto.
- Se creo una nota de memoria en `.codex/memories/...` con contexto del repo.
- Se leyo `docs/08-commits.md`.
- Se hicieron commits atomicos listados arriba.
- Se hizo push a `origin/main`.

---

## 7. Cambios recientes no commiteados - Docker, datos y seeder

El usuario pidio trabajar primero en infraestructura de datos:

- Que `docker compose down` deje el entorno limpio.
- Que `docker compose up --build` ejecute el seed completo automaticamente.
- Que el seeder sea idempotente, validado y rico.
- Poblar con mucha informacion conectada y datos realistas de LoL, CS2 y Valorant.
- Corregir manual en Docker.
- Evaluar si validaciones/select/login/inscripcion aplicaban a este fix para evitar ensuciar Cassandra.

### 7.1 `docker-compose.yml`

Cambios hechos:

- Se quito el volumen nombrado persistente de Cassandra (`cassandra-data`).
- Se elimino el bloque `volumes:` del final.
- `seeder` ya no esta en profile manual `seed`.
- `seeder` depende de `gateway` healthy.
- `frontend` depende de `seeder: condition: service_completed_successfully`.
- `tests` depende de `seeder: condition: service_completed_successfully`.

Efecto:

- `docker compose down` baja el stack y el siguiente `docker compose up --build` arranca Cassandra limpia.
- El seeder repuebla automaticamente.
- El frontend queda disponible despues de que el seed termina.

Nota:

- Puede existir un volumen viejo de Docker de configuraciones anteriores, pero ya no se usa por el compose actual.

### 7.2 Documentacion Docker/setup

Archivos ajustados:

- `README.MD`
- `docs/06-docker-setup.md`
- `docs/07-plan-ejecucion.md`
- `AGENTS.md`
- `CLAUDE.md`
- `frontend/MANUAL-USUARIO.md`

Cambios:

- Se removio la instruccion de seed manual.
- Se documenta que `docker compose up --build` corre seeder automaticamente.
- Se documenta que `docker compose down` deja el siguiente arranque limpio.
- Se actualizo el conteo del dataset demo.
- Se actualizo el manual para explicar el seed automatico.

### 7.3 Frontend manual en Docker

Problema previo:

- `frontend/.dockerignore` excluia `*.md`.
- `frontend/MANUAL-USUARIO.md` no entraba en la imagen Docker.
- `/manual` podia mostrar "Manual no encontrado".

Fix:

- Se removio `*.md` de `frontend/.dockerignore`.
- `frontend/Dockerfile` copia `MANUAL-USUARIO.md` al runtime standalone:
  - `COPY --from=build /app/MANUAL-USUARIO.md ./MANUAL-USUARIO.md`

Verificado:

- `curl http://localhost:3000/manual` ya no muestra "Manual no encontrado".
- El manual renderiza texto de seed automatico y dataset.

### 7.4 Seeder

Archivo principal:

- `tools/Esports.Seeder/Program.cs`

Se rehizo de forma importante:

- Espera health del gateway.
- Usa helpers robustos:
  - `GetOptionalAsync`
  - `PostAsync`
  - `PostNoBodyAsync`
- Todo POST valida status.
- Si una request falla, lanza error con path/status/body.
- Ya no hay POSTs silenciosos.

Idempotencia:

- Videojuegos: lookup por genero/nombre.
- Organizadores: lookup por lista/nombre.
- Equipos: lookup por tag.
- Jugadores: lookup por nickname.
- Torneos: lookup por codigo.
- Inscripciones: revisa lista de equipos del torneo.
- Premios: revisa lista de premios del torneo.
- Partidas: revisa torneo/local/visitante/resultado/fecha.

Dataset actual:

- 5 videojuegos:
  - League of Legends
  - Valorant
  - Counter-Strike 2
  - Dota 2
  - Rocket League
- 7 organizadores:
  - Riot Games
  - LoL Esports
  - VALORANT Champions Tour
  - ESL FACEIT Group
  - BLAST Premier
  - PGL
  - UNIVALLE Esports
- 40 equipos.
- 200 jugadores generados.
- 12 torneos.
- Inscripciones, premios, partidas, ranking de equipos, victorias, jugadores y stats.

Torneos incluidos:

- `MSI26` League of Legends MSI 2026
- `WORLDS25` League of Legends Worlds 2025
- `LEC-SUM26` LEC Summer 2026
- `VCT-CHAMP25` VALORANT Champions Paris 2025
- `VCT-AMER-S2-25`
- `VCT-EMEA-S2-25`
- `MASTERS-TOR25`
- `IEM-COL26`
- `BLAST-AUS25`
- `BLAST-PORTO26`
- `BLAST-FW26`
- `UNI-INV26`

Fuentes externas usadas para nombres/contexto realista:

- LoL Esports MSI 2026 Primer: `https://lolesports.com/news/msi-2026-primer`
- VALORANT Champions Paris standings: `https://valorantesports.com/en-GB/tournament/113482263742879102/overview`
- BLAST.tv Austin Major 2025: `https://blast.tv/cs/tournaments/austin-major-2025-finals`

Importante:

- El dataset es demo curado, no sincronizacion en vivo.
- Hay nombres realistas y equipos reales/recientes, pero la data no pretende ser una base oficial historica.

### 7.5 Verificacion de Docker/seeder

Se verifico:

```bash
docker compose config --quiet
docker compose build seeder teams tournaments matches frontend
docker compose down --remove-orphans
docker compose up --build -d
docker compose ps --all
```

Estado observado:

- `cassandra` healthy.
- `rabbitmq` healthy.
- `teams`, `tournaments`, `matches`, `ranking`, `gateway`, `frontend` healthy.
- `seeder` `Exited (0)`.

Conteos verificados via gateway:

- `equipos = 40`
- `torneos = 12`
- `organizadores = 7`
- `ranking_equipos = 40`
- `ranking_victorias = 40`
- `ranking_jugadores = 200`
- basura/test tags o nombres vacios = `0`

Smoke Q1-Q24 via gateway paso con datos.

Ejemplos:

- Q1 `T1Ghost`
- Q2 pais `KR` devuelve 21 jugadores
- Q7 ranking equipos devuelve datos
- Q23 ranking jugadores devuelve IDs + totalTorneos
- Q24 stats de equipo/torneo devuelve victorias, derrotas y partidas jugadas

### 7.6 Verificacion no realizada

No se pudo correr `dotnet build Esports.sln` localmente porque en la maquina no hay `dotnet` en PATH:

```text
zsh:1: command not found: dotnet
```

Se uso Docker para build/verificacion.

`docker compose build tests` paso, pero no se corrio la suite completa porque los tests actuales todavia contienen pruebas antiguas que crean datos como `Test Team` o esperan fixtures anteriores. Ejecutarlos sobre la demo actual puede ensuciar Cassandra.

---

## 8. Cambios recientes no commiteados - validaciones backend/datos limpios

El usuario pregunto si limpiar bugs de select/login/inscripcion y validaciones aplicaba al fix de datos. Se determino que la parte de validaciones si aplica al bloque de datos, porque evita ensuciar Cassandra.

Cambios hechos:

### 8.1 Teams

Archivo:

- `services/teams/Esports.Teams.Api/Dtos/EquipoDto.cs`

Se agregaron DataAnnotations a:

- `CrearEquipoRequest`
- `AgregarJugadorRequest`

Validaciones:

- Required.
- No aceptar strings solo espacios con regex.
- MaxLength en campos relevantes.

Archivo:

- `services/teams/Esports.Teams.Api/Services/EquipoService.cs`

Se normaliza:

- `nombre.Trim()`
- `tag.Trim().ToUpperInvariant()`
- `pais.Trim().ToUpperInvariant()`
- `nickname.Trim()`
- `rol.Trim().ToUpperInvariant()`

### 8.2 Tournaments

Archivo:

- `services/tournaments/Esports.Tournaments.Api/Dtos/Dtos.cs`

Se agregaron DataAnnotations a:

- `CrearVideojuegoRequest`
- `CrearOrganizadorRequest`
- `CrearTorneoRequest`
- `AsignarPremioRequest`

Archivos:

- `VideojuegoService.cs`
- `OrganizadorService.cs`
- `TorneoService.cs`

Se hace trim y normalizacion (`genero`, `codigo`, etc.).

Nota importante:

- Al principio se aplicaron atributos como `[property:]` sobre records, lo cual provoco 500 runtime en ASP.NET.
- Se corrigio quitando `property:` para que model binding/validation funcione correctamente.

### 8.3 Matches

Archivos:

- `services/matches/Esports.Matches.Api/Dtos/Dtos.cs`
- `services/matches/Esports.Matches.Api/Services/PartidaService.cs`
- `services/matches/Esports.Matches.Api/Controllers/PartidasController.cs`

Validaciones:

- Strings requeridos.
- Rechazar GUID vacios.
- Rechazar local == visitante.
- Rechazar ganador que no sea local ni visitante.
- Trim de nombres/resultados.
- `ArgumentException` se traduce a 400 `ProblemDetails`.

### 8.4 Tests

Archivo:

- `tests/Esports.Gateway.Tests/ErrorTests.cs`

Se actualizo una prueba para que `POST_CrearEquipo_NombreVacio_Devuelve400` espere `400 BadRequest`, no `201 Created`.

Pendiente:

- La suite completa requiere alineacion con el seed nuevo y aislamiento para no ensuciar la demo.

---

## 9. Auditoria frontend realizada

El usuario reporto:

- Ranking de jugadores muestra ID y no nombre.
- En `/jugadores` no aparece ningun jugador.
- Cambio de rol no cambia la pantalla.
- Entrar como organizador, por ejemplo Riot Games, preocupa porque no hay backend de roles y podria hacer todo.
- Se pidio auditar frontend con navegador y codigo, usando subagentes si era posible.

Se lanzaron dos subagentes:

1. Auditoria roles/login/navegacion/home.
2. Auditoria jugadores/rankings/API.

Tambien se verifico manualmente con navegador en vivo.

---

## 10. Problema frontend - ranking de jugadores muestra IDs

### Causa raiz

Endpoint:

```bash
curl 'http://localhost:8080/api/ranking/jugadores?top=5'
```

Respuesta:

```json
[
  { "jugadorId": "041ede23-ce38-4c05-904f-65e09d6c0cc3", "totalTorneos": 5 }
]
```

El backend de `ranking` solo devuelve `jugadorId` y `totalTorneos`.

Esto es correcto por arquitectura:

- `ranking_jugadores_activos` usa counters.
- En Cassandra, una tabla con counter solo puede tener primary key + counters.
- No puede guardar `nombre_jugador` junto al counter.
- `docs/02-modelo-datos.md` lo explica: ranking devuelve id + contador y el nombre se resuelve aparte por REST o cache.

El frontend actualmente:

- Para ranking de equipos, resuelve nombres con `getEquipoPorId`.
- Para ranking de jugadores, renderiza `shortId(r.jugadorId)`.
- El texto dice "Los IDs se resuelven como nombres...", pero eso no esta implementado.
- `frontend/src/lib/api/ranking.ts` tiene `nombreJugador?`, pero ese campo nunca llega de la API.

### Solucion recomendada

No meter nombres en `ranking_jugadores_activos`.

Implementar lookup auxiliar en `teams`:

- `GET /api/jugadores/{jugadorId}`
- Lee tabla base `jugadores`, que ya existe.
- Agregar metodo en:
  - `JugadorRepository`
  - `JugadorService`
  - `JugadoresController`
  - `frontend/src/lib/api/equipos.ts`
- En `frontend/src/app/rankings/page.tsx`, crear componente `JugadorNombre`, analogo a `EquipoNombre`.

Actualizar `docs/04-contratos-api.md` indicando que `GET /api/jugadores/{id}` es endpoint auxiliar por id, no una Q nueva.

---

## 11. Problema frontend - `/jugadores` parece vacio

### Evidencia

Gateway tiene datos:

```bash
curl http://localhost:8080/api/jugadores/por-pais/KR
# Devuelve 21 jugadores.

curl http://localhost:8080/api/jugadores/por-nickname/T1Ghost
# Devuelve jugador valido.
```

Navegador:

- `/jugadores` carga solo con tabs:
  - Por nickname (Q1)
  - Por pais (Q2)
- No hay listado inicial.
- Las queries estan deshabilitadas hasta que el usuario busque (`enabled: !!query`).

### Causa raiz

No es problema del seeder ni de Cassandra.

La pantalla esta implementada como buscador, no como listado.

Ademas, hay mismatch de UX:

- El seed usa paises tipo codigo ISO: `KR`, `CN`, `US`, `DK`, etc.
- La UI sugiere "Bolivia, Korea".
- Si el usuario escribe `Korea`, probablemente obtiene vacio.
- Si escribe `KR`, aparecen datos.

### Fix recomendado

P0:

- Normalizar parametros de lectura: `pais.Trim().ToUpperInvariant()` en backend y/o frontend.
- Cambiar placeholder a ejemplos reales: `KR`, `CN`, `US`, `DK`.
- Agregar chips rapidos de paises frecuentes para que la pagina muestre resultados facil.

P1:

- Mostrar una seccion inicial de "jugadores destacados" usando Q23 enriquecido con `GET /api/jugadores/{id}`.

P2:

- Si realmente se quiere listado global, agregar `GET /api/jugadores`, pero documentarlo como endpoint auxiliar. No esta dentro de Q1-Q24 originales.

---

## 12. Problema frontend - roles/login/home

### Estado actual

Auth es demo-only en frontend:

- `frontend/src/lib/auth/context.tsx`
  - Guarda identidad en `localStorage`.
  - Key: `esports-identidad`.
  - Roles: `organizador`, `capitan`, `fan`.

- `frontend/src/app/login/page.tsx`
  - Permite elegir rol.
  - Organizador elige un organizador existente.
  - Capitan elige un equipo existente.
  - Fan no elige entidad.
  - Luego hace `router.push("/")`.

- `frontend/src/app/api/[...path]/route.ts`
  - Proxy local a `GATEWAY_URL`.
  - No agrega token, rol ni claims.

- Backend:
  - No valida rol.
  - No valida ownership.
  - Cualquier POST publico puede ejecutarse si se llama al endpoint.

### Problema visual

El estado de rol si cambia, pero la home no lo usa.

Verificado entrando como Organizador/Riot Games:

- Navbar muestra `Riot Games`.
- Home sigue mostrando la misma landing.
- Home mantiene CTA `Ingresar como organizador o capitan`.
- No aparece dashboard de organizador.
- No hay pagina distinta por rol.

### Preocupacion explicita del usuario

El usuario esta preocupado por esto:

- Si elige Riot Games, la app deberia comportarse como "estoy actuando como Riot Games".
- Hoy no hay servicio de auth/roles.
- Como no hay backend de roles, Riot Games podria hacer cualquier cosa si la UI lo permite o si alguien llama la API.
- Cada rol necesita un frontend/pagina/diseño propio:
  - que muestre lo que puede hacer,
  - lo que no puede hacer,
  - flujos de sus historias,
  - acciones principales,
  - restricciones de contexto.

Esta preocupacion debe guiar la siguiente tanda de frontend.

---

## 13. Decision pendiente - demo honesta vs auth real

Hay dos caminos:

### Camino A - Demo honesta y bien disenada (recomendado ahora)

Mantener auth solo frontend porque el foco del proyecto es microservicios distribuidos, Cassandra, gateway y eventos.

Pero hacer que la UI sea coherente:

- Home role-aware.
- Dashboards por rol.
- Acciones escondidas o deshabilitadas segun rol.
- Organizador elegido restringe la UI a ese organizador.
- Capitan elegido restringe la UI a ese equipo.
- Mensajes claros: "Modo demo: el rol controla la UI, no es seguridad backend".
- Manual actualizado con esta limitacion.

### Camino B - Auth real

Implementar auth backend:

- Servicio/endpoint de auth.
- Tokens/sesion.
- Claims de rol y entidad.
- Validacion de ownership en backend.
- Middleware/autorizacion.
- Tests de seguridad.

Este camino es mas correcto en produccion pero grande para el deadline. No se recomendo para este momento salvo que el usuario lo pida explicitamente.

---

## 14. Diseno esperado por rol

El usuario quiere que cada rol tenga una experiencia clara, no solo una etiqueta en navbar.

### Fan / Visitante

Debe ser solo lectura.

Pantalla/home:

- Explorar torneos.
- Ver rankings.
- Buscar equipos.
- Buscar jugadores.
- Ver partidas y enfrentamientos.
- Ver organizadores/videojuegos.

No debe ver CTAs de escritura:

- Crear torneo.
- Crear videojuego.
- Crear organizador.
- Registrar partida.
- Asignar premio.
- Crear equipo.
- Agregar jugador.
- Inscribir equipo.

### Organizador

Debe actuar como el organizador elegido en login.

Ejemplo: si elige `Riot Games`, la UI debe decir claramente "Actuando como Riot Games".

Pantalla/home:

- Dashboard de organizador.
- Torneos de ese organizador.
- Accion para crear torneo preseleccionando/bloqueando ese organizador.
- Accion para registrar partidas en torneos que organiza.
- Accion para asignar premios en torneos que organiza.
- Accion para crear videojuegos si se permite como rol organizador global.
- Crear organizador nuevo debe discutirse: quizas no deberia estar disponible para "Riot Games"; podria ser una accion admin/demo separada.

Restricciones UI recomendadas:

- En `CrearTorneoDialog`, bloquear `organizadorId` al de la identidad.
- En detalle de torneo, solo permitir registrar partida/asignar premio si `torneo.organizadorId == identidad.organizadorId` o si no existe ese dato en el DTO, agregarlo/usar endpoint para verificar.
- No mostrar acciones de organizador en torneos ajenos.
- No mostrar "crear organizador" como accion normal de un organizador representante, salvo que sea modo admin/demo.

### Capitan

Debe actuar como el equipo elegido en login.

Pantalla/home:

- Dashboard de mi equipo.
- Ver roster.
- Agregar jugador a mi equipo.
- Ver torneos donde participa mi equipo.
- Inscribir mi equipo a torneos disponibles.
- Ver partidas, premios y stats de mi equipo.

Restricciones UI recomendadas:

- `Crear equipo` debe ser flujo "crear mi equipo" y actualizar identidad al nuevo equipo, o no mostrarse si ya eligio equipo.
- En inscripcion de torneo, preseleccionar y restringir a `identidad.equipoId`.
- No listar todos los equipos como inscribibles para un capitan.
- No permitir agregar jugadores a equipos ajenos.

---

## 15. Bugs/deudas frontend encontradas

P0:

- Home no es role-aware.
- Ranking jugadores muestra IDs.
- `/jugadores` no muestra nada de entrada y ejemplos de pais no coinciden con seed.
- Falta accion visible "Cambiar rol".
- El usuario logueado todavia ve CTA "Ingresar como organizador o capitan".

P1:

- Organizador puede crear torneo bajo cualquier organizador porque selector queda editable.
- Capitan puede seleccionar cualquier equipo en login. Como demo puede ser aceptable, pero debe comunicarse.
- Capitan puede crear equipo pero identidad no se actualiza al nuevo equipo.
- Inscripcion de torneo calcula `miEquipoId`, pero `Select` controla `equipoId = ""`; `defaultValue` queda ignorado y lista todos los equipos.
- En premios, `SelectItem value=""` para "Sin asignar" puede romper con Radix Select; usar sentinel como `sin-asignar`.
- Login usa cards clickeables sin semantica de boton.
- AuthProvider arranca con `identidad = null` y luego lee localStorage; puede haber flicker.
- Tras `docker compose down` + nuevo seed, la identidad guardada puede apuntar a IDs viejos; hay que validar o limpiar identidad al cargar si entidad no existe.

P2:

- Navbar no filtra links por rol.
- En mobile el label de rol puede quedar oculto.
- Tipos TS tienen campos opcionales (`nombreJugador?`, `nombreEquipo?`) que la API no entrega.
- Tests frontend no existen.

---

## 16. Tests y estado de calidad

Tests existentes:

- `tests/Esports.Gateway.Tests`
- 97 tests de integracion via gateway en commits previos.

Problema:

- Fueron escritos contra un seed anterior.
- Algunos esperan `Faker`, `s1mple` u otros datos anteriores.
- Algunos tests mutan datos creando entidades tipo `Test Team`.
- Si se corren contra la demo actual pueden ensuciar Cassandra.

Verificado recientemente:

- `docker compose build tests` pasa.
- No se corrio `docker compose --profile test ...` completo por riesgo de ensuciar datos y porque debe actualizarse suite.

Siguiente fix recomendado:

- Aislar tests en un compose project o keyspace/data reset propio.
- Actualizar fixtures a seed actual o reintroducir jugadores conocidos estables.
- Evitar que tests de error creen datos validos persistentes.

---

## 17. CI/CD

No se encontro pipeline `.github/workflows` ni archivo CI/CD formal en el repo.

Lo que existe hoy:

- Docker Compose como flujo reproducible local.
- Dockerfiles por servicio.
- Tests Dockerizados.
- No hay GitHub Actions u otro pipeline configurado.

Si el usuario dice "CD" refiriendose a CI/CD, aun falta crear pipeline. Si se refiere a Docker Compose/continuous delivery local, el cambio relevante actual es que Compose ya orquesta seed automatico y frontend despues de seed.

---

## 18. Verificacion actual del stack

`docker compose ps --all` reciente:

- `esports-cassandra`: healthy.
- `esports-rabbitmq`: healthy.
- `esports-teams`: healthy.
- `esports-tournaments`: healthy.
- `esports-matches`: healthy.
- `esports-ranking`: healthy.
- `esports-gateway`: healthy.
- `esports-frontend`: healthy.
- `esports-seeder`: `Exited (0)`.

Puertos:

- Frontend: `http://localhost:3000`
- Gateway: `http://localhost:8080`
- Teams: `http://localhost:5001`
- Tournaments: `http://localhost:5002`
- Matches: `http://localhost:5003`
- Ranking: `http://localhost:5004`
- RabbitMQ Management: `http://localhost:15672`

---

## 19. Orden recomendado de trabajo desde aqui

### Bloque 1 - Commitar lo ya hecho

Antes de seguir cambiando logica, conviene commitear atomicamente los cambios locales actuales.

Propuesta de commits:

1. `build(infra): run seeder automatically after clean compose startup`
   - `docker-compose.yml`
   - partes de docs setup si se decide incluir en mismo commit o separar.

2. `feat(seed): load rich idempotent esports demo dataset`
   - `tools/Esports.Seeder/Program.cs`

3. `fix(frontend): include user manual in Docker image`
   - `frontend/.dockerignore`
   - `frontend/Dockerfile`
   - `frontend/MANUAL-USUARIO.md` si el cambio es manual runtime.

4. `fix(api): reject invalid demo data inputs`
   - Validaciones en teams/tournaments/matches.
   - `tests/Esports.Gateway.Tests/ErrorTests.cs`

5. `docs: document automatic seed and clean compose reset`
   - `README.MD`
   - `docs/06-docker-setup.md`
   - `docs/07-plan-ejecucion.md`
   - `AGENTS.md`
   - `CLAUDE.md`

6. `docs: add comprehensive project handoff`
   - `Handoff.md`

Pueden ajustarse scopes segun diff exacto. Respetar `docs/08-commits.md`.

### Bloque 2 - Ranking jugadores y pagina jugadores

Implementar:

- `GET /api/jugadores/{jugadorId}` en teams.
- Cliente `getJugadorPorId`.
- `JugadorNombre` en ranking.
- Placeholder/chips de pais.
- Normalizacion de pais en lectura.
- Vista inicial de jugadores destacados usando Q23 enriquecido.

### Bloque 3 - Home y roles

Implementar:

- Home role-aware.
- Dashboard por rol.
- CTA correcto segun identidad.
- Boton "Cambiar rol".
- Mostrar "Actuando como X".
- Fan sin acciones de escritura.
- Organizador con acciones de organizador.
- Capitan con acciones de su equipo.

### Bloque 4 - Ownership demo y formularios

Implementar:

- Organizador elegido bloquea selector en crear torneo.
- Organizador solo ve/puede actuar en torneos propios.
- Capitan solo inscribe su equipo.
- Crear equipo como capitan actualiza identidad o se rediseña como "crear mi primer equipo".
- Reemplazar `SelectItem value=""`.
- Validar identidad persistida contra datos actuales.

### Bloque 5 - Tests

Implementar:

- Tests actualizados al seed actual.
- Tests aislados para no ensuciar demo.
- Smoke E2E frontend si se puede.

---

## 20. Archivos relevantes para proximos cambios

Backend teams:

- `services/teams/Esports.Teams.Api/Controllers/JugadoresController.cs`
- `services/teams/Esports.Teams.Api/Services/JugadorService.cs`
- `services/teams/Esports.Teams.Api/Repositories/JugadorRepository.cs`
- `services/teams/Esports.Teams.Api/Dtos/EquipoDto.cs`
- `services/teams/Esports.Teams.Api/Cassandra/SchemaInitializer.cs`

Backend ranking:

- `services/ranking/Esports.Ranking.Api/Controllers/RankingController.cs`
- `services/ranking/Esports.Ranking.Api/Dtos/Dtos.cs`
- `services/ranking/Esports.Ranking.Api/Repositories/RankingRepository.cs`

Frontend:

- `frontend/src/lib/api/equipos.ts`
- `frontend/src/lib/api/ranking.ts`
- `frontend/src/lib/auth/context.tsx`
- `frontend/src/lib/auth/types.ts`
- `frontend/src/components/layout/navbar.tsx`
- `frontend/src/app/page.tsx`
- `frontend/src/app/login/page.tsx`
- `frontend/src/app/jugadores/page.tsx`
- `frontend/src/app/rankings/page.tsx`
- `frontend/src/app/equipos/page.tsx`
- `frontend/src/app/equipos/[id]/page.tsx`
- `frontend/src/app/torneos/page.tsx`
- `frontend/src/app/torneos/[id]/page.tsx`
- `frontend/src/app/videojuegos/page.tsx`
- `frontend/src/app/organizadores/page.tsx`

Seeder/Docker:

- `tools/Esports.Seeder/Program.cs`
- `docker-compose.yml`
- `frontend/Dockerfile`
- `frontend/.dockerignore`
- `frontend/MANUAL-USUARIO.md`

Docs:

- `AGENTS.md`
- `CLAUDE.md`
- `README.MD`
- `docs/02-modelo-datos.md`
- `docs/04-contratos-api.md`
- `docs/06-docker-setup.md`
- `docs/07-plan-ejecucion.md`
- `docs/08-commits.md`

Tests:

- `tests/Esports.Gateway.Tests/TeamsTests.cs`
- `tests/Esports.Gateway.Tests/TournamentsTests.cs`
- `tests/Esports.Gateway.Tests/MatchesTests.cs`
- `tests/Esports.Gateway.Tests/RankingTests.cs`
- `tests/Esports.Gateway.Tests/ErrorTests.cs`
- `tests/Esports.Gateway.Tests/GatewayFixture.cs`

---

## 21. Comandos utiles

Arranque limpio:

```bash
docker compose down
docker compose up --build
```

Ver estado:

```bash
docker compose ps --all
docker compose logs -f seeder
docker compose logs -f frontend
docker compose logs -f gateway
```

Endpoints rapidos:

```bash
curl -fsS http://localhost:8080/api/equipos/por-fecha | jq length
curl -fsS http://localhost:8080/api/torneos/por-fecha | jq length
curl -fsS http://localhost:8080/api/organizadores | jq length
curl -fsS 'http://localhost:8080/api/jugadores/por-pais/KR' | jq length
curl -fsS 'http://localhost:8080/api/ranking/jugadores?top=5' | jq .
curl -fsS http://localhost:3000/manual | rg 'Manual no encontrado|seeder corre automaticamente|40 equipos'
```

Git:

```bash
git status --short --branch
git log --oneline --decorate -n 20
git diff --stat
```

---

## 22. Cosas que NO debe hacer el proximo agente

- No revertir cambios locales sin preguntar.
- No meter nombres en tablas counter de ranking.
- No hacer que `ranking` lea el keyspace de `teams`.
- No convertir auth en un sistema enorme de passwords/registro/recuperacion si el usuario no lo pide. La decision actual es implementar un microservicio de auth minimo y profesional con identidad demo, JWT y autorizacion real por rol/ownership.
- No correr tests mutantes contra demo si van a ensuciar Cassandra.
- No volver a hacer seed manual como flujo principal.
- No usar `docker compose down -v` como requisito para reset si el objetivo actual es que `down` simple sea suficiente.
- No cambiar MassTransit a 9.x.
- No instalar EF.

---

## 23. Resumen ejecutivo para continuar

El repo ya tiene backend completo, frontend demo, compose y commits pusheados hasta `8e58d4d`. La tanda actual dejo commits locales para que Docker arranque limpio y auto-seedee, con dataset rico e idempotente, manual en Docker corregido y validaciones backend para evitar datos sucios. Falta el commit final de documentacion/handoff y luego push si el usuario lo pide.

La auditoria frontend encontro que el problema de jugadores no es de datos: hay 200 jugadores y Q2 funciona con codigos como `KR`. El problema es de UX/contrato: `/jugadores` no lista de entrada y Q23 solo devuelve IDs. La solucion limpia es agregar lookup de jugador por ID en `teams` y enriquecer el ranking en frontend.

La preocupacion mas importante del usuario ahora es roles/ownership. Hoy el rol solo vive en frontend y no hay seguridad backend. El usuario decidio que, aunque se habia evitado para reducir complejidad, lo mejor es integrar un microservicio de autenticacion antes de seguir arreglando frontend, porque simular todo en React aumentaria el trabajo y mantendria inconsistencias. El siguiente agente debe implementar auth minimo con JWT y autorizacion real por rol/ownership, y luego adaptar frontend, docs, compose, gateway, servicios y tests.

---

## 24. Prompt profesional para el siguiente agente - microservicio de autenticacion

Usar este prompt para iniciar una nueva conversacion/agente que implemente la feature de autenticacion. El objetivo es que el agente entienda que no es solo agregar un servicio, sino actualizar arquitectura, documentacion, Compose, gateway, servicios, frontend y tests sin romper lo que ya funciona.

```text
Trabaja en el repo `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`.

Necesito que implementes un microservicio de autenticacion/autorization para la plataforma de eSports. Antes de tocar codigo, lee obligatoriamente:

- `AGENTS.md`
- `CLAUDE.md`
- `Handoff.md`
- `README.MD`
- `USER-STORIES.md`
- `docs/01-arquitectura.md`
- `docs/02-modelo-datos.md`
- `docs/03-convenciones.md`
- `docs/04-contratos-api.md`
- `docs/05-eventos.md`
- `docs/06-docker-setup.md`
- `docs/07-plan-ejecucion.md`
- `docs/08-commits.md`
- `frontend/MANUAL-USUARIO.md`

Contexto importante:

- El sistema actual tiene 4 microservicios .NET 10 (`teams`, `tournaments`, `matches`, `ranking`) + `gateway` YARP + frontend Next.js.
- Cassandra es query-first/Chebotko. Cada servicio tiene su keyspace. No hacer cross-keyspace queries.
- `ranking` usa counters; no meter nombres en tablas counter.
- Docker Compose debe seguir siendo el flujo principal. `docker compose down` debe dejar el proximo arranque limpio y `docker compose up --build` debe repoblar con seeder automaticamente.
- Ya existe frontend con login demo via localStorage (`esports-identidad`), pero eso NO es auth real.
- El usuario decidio que se debe asumir la complejidad de un microservicio de auth porque simular permisos solo en frontend genera mas trabajo y riesgos.

Objetivo de la feature:

Agregar un quinto microservicio backend llamado `auth` que emita identidad autenticada para demo academica y permita autorizacion real por rol/ownership en los servicios. No construir un sistema enorme de passwords, registro publico, recovery, email, OAuth, etc. Construir auth minimo, profesional, defendible y suficiente para el proyecto.

Roles esperados:

- `fan`: solo lectura.
- `organizador`: puede actuar solamente como el organizador elegido.
- `capitan`: puede actuar solamente como el equipo elegido.

Modelo de identidad esperado:

- Login demo sin password real:
  - fan: no requiere entidad.
  - organizador: elige un organizador existente.
  - capitan: elige un equipo existente.
- Auth devuelve JWT firmado con claims:
  - `sub`
  - `role`
  - `organizador_id` cuando role = organizador
  - `equipo_id` cuando role = capitan
  - nombre/tag display para frontend si conviene
  - expiracion razonable
- Secret/config por variables de entorno, no hardcodeado.

Arquitectura esperada:

1. Crear `services/auth/Esports.Auth.Api`.
2. Agregarlo a `Esports.sln`.
3. Agregar Dockerfile.
4. Agregarlo a `docker-compose.yml` con healthcheck y puerto host dev nuevo, sugerido `5005`, interno `8080`.
5. Agregar ruta en gateway YARP para `/api/auth/**`.
6. Configurar JWT validation en gateway y/o servicios segun convenga, pero las autorizaciones de negocio deben estar en los servicios dueños de la mutacion. El gateway puede validar token basico; los servicios deben validar ownership.
7. No romper los endpoints publicos de lectura. Lecturas deben seguir accesibles a fan/anonimas segun se decida.
8. Las mutaciones deben requerir token y rol correcto.

Reglas de autorizacion minima:

Teams:

- `POST /api/equipos`:
  - Puede ser `capitan` para crear su equipo si el flujo lo soporta, pero si crea un equipo nuevo debe haber regla clara de ownership.
  - Alternativa: dejar creacion de equipo como demo/admin si no se puede resolver ownership correctamente; documentar decision.
- `POST /api/equipos/{equipoId}/jugadores`:
  - Solo `capitan` con `equipo_id == equipoId`.

Tournaments:

- `POST /api/videojuegos`:
  - Solo `organizador` o decidir si requiere rol admin/demo. Documentar.
- `POST /api/organizadores`:
  - Probablemente NO debe estar disponible para un organizador normal. Considerar bloquearlo o tratarlo como admin/demo. Documentar decision.
- `POST /api/torneos`:
  - Solo `organizador`.
  - `organizadorId` del request debe coincidir con claim `organizador_id`. Si no coincide, devolver 403 ProblemDetails.
- `POST /api/torneos/{torneoId}/inscripciones`:
  - Solo `capitan`.
  - `equipoId` del request debe coincidir con claim `equipo_id`.
- `POST /api/torneos/{torneoId}/premios`:
  - Solo `organizador` dueño del torneo.
- Cualquier operacion sobre torneo como organizador debe verificar que el torneo pertenezca a `organizador_id` del token.

Matches:

- `POST /api/partidas`:
  - Solo `organizador` dueño del torneo.
  - `matches` actualmente no es dueño de torneos. Debe consultar a `tournaments` por REST tipado para verificar ownership o recibir un endpoint de validacion desde `tournaments`.
  - No hacer cross-keyspace.

Ranking:

- Sigue read-only/publico.

Frontend:

- Reemplazar login demo localStorage por login contra `auth`.
- Guardar token y perfil de forma segura para demo (localStorage aceptable si se documenta como demo; idealmente manejar expiracion).
- Agregar `Authorization: Bearer <token>` en el proxy/fetcher.
- La UI debe ser role-aware:
  - fan: solo lectura.
  - organizador: dashboard "Actuando como <organizador>", acciones de sus torneos, crear torneo bloqueado a su organizador, registrar partidas/premios solo en torneos propios.
  - capitan: dashboard "Mi equipo", agregar jugadores solo a su equipo, inscribir solo su equipo.
- Agregar boton visible "Cambiar rol" o logout/login.
- Si el token expira o la entidad ya no existe tras reset, limpiar sesion y mandar a login.

Documentacion que debes actualizar:

- `AGENTS.md`
- `CLAUDE.md`
- `README.MD`
- `Handoff.md`
- `docs/01-arquitectura.md`
- `docs/03-convenciones.md`
- `docs/04-contratos-api.md`
- `docs/06-docker-setup.md`
- `docs/07-plan-ejecucion.md`
- `frontend/MANUAL-USUARIO.md`
- Si agregas docs nuevas, mantener formato y coherencia.

Tests:

- Actualizar/agregar integration tests para auth:
  - login fan/organizador/capitan.
  - mutaciones sin token -> 401.
  - mutaciones con rol incorrecto -> 403.
  - organizador no puede crear torneo para otro organizador.
  - capitan no puede inscribir otro equipo.
  - organizador no puede registrar partida/asignar premio en torneo ajeno.
  - lecturas publicas siguen funcionando.
- Evitar que tests ensucien la demo. Si mutan, deben usar datos controlados o entorno aislado.
- Actualizar tests viejos que asumian seed anterior si fallan por fixtures.

Verificacion obligatoria antes de finalizar:

1. `docker compose config --quiet`
2. `docker compose build auth gateway teams tournaments matches ranking frontend seeder`
3. `docker compose down --remove-orphans`
4. `docker compose up --build -d`
5. Confirmar `docker compose ps --all` con servicios healthy y seeder `Exited (0)`.
6. Probar login auth por curl:
   - fan
   - organizador
   - capitan
7. Probar 401/403 por curl en al menos:
   - crear torneo sin token
   - crear torneo con organizador incorrecto
   - inscribir equipo ajeno como capitan
8. Smoke Q1-Q24 via gateway.
9. Verificar frontend en navegador:
   - login por rol
   - dashboard cambia por rol
   - Riot Games solo actua como Riot Games
   - capitan solo actua como su equipo
   - fan no ve acciones de escritura
10. Ejecutar tests que correspondan o explicar claramente si alguno no se pudo ejecutar.

Restricciones:

- No cambiar versiones de stack sin avisar.
- No usar EF.
- No instalar MassTransit 9.x.
- No meter nombres en tablas counter.
- No hacer cross-keyspace.
- No romper el auto-seed ni el flujo de `docker compose up --build`.
- No esconder problemas de autorizacion solo en frontend; las reglas de negocio deben protegerse en backend.
- No hacer commits gigantes mezclados. Respetar `docs/08-commits.md`.

Entregable esperado:

- Codigo implementado.
- Docs actualizadas.
- Tests actualizados.
- Informe final en espanol con:
  - resumen de arquitectura auth,
  - endpoints nuevos,
  - reglas 401/403,
  - verificaciones ejecutadas,
  - estado git,
  - riesgos pendientes.
```
