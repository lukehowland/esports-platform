# Handoff — Esports Platform: auth/RBAC, integridad y showcase live

Fecha: 2026-06-17
Repo: `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`
Rama de trabajo: `codex/restrict-videogames-and-match-enrollment`
Generado por: Claude + Codex

Este handoff reemplaza al anterior de PR #6. Documenta el estado actual tras el merge del CRUD
admin a `main` y la rama posterior de Codex para reforzar integridad de datos y agregar el
showcase live del home público: videojuegos admin-only, partidas/premios solo para equipos
inscritos, seed nuevo con Gen.G + `RIFT-LIVE26`, y endpoint efimero de partida en vivo.

---

## ⚠️ ESTADO FINAL — QUÉ NO HACER AL RETOMAR

- [ ] **NO reimplementar el CRUD admin.** Organizadores (edit/delete), videojuegos
  (show-all + filtro por chips, crear/editar en modal compartido, delete), usuarios
  (lista + alta-modal + baja) ya están completos y mergeados (PR #6, squash `1cffff3`).
- [ ] **NO quitar el bloqueo 409 "block-on-dependents".** Borrar/editar un organizador o un
  videojuego que **todavía tiene torneos** devuelve `409 ProblemDetails` a propósito. El detalle
  del 409 se muestra en un toast en el frontend. Es regla de negocio, no un bug.
- [ ] **NO permitir self-delete ni borrar el admin bootstrap.** `DELETE /api/auth/usuarios/{u}`
  guarda contra borrarte a vos mismo y contra borrar el admin de arranque (409). La fila propia
  está deshabilitada en la UI. Deliberado.
- [ ] **NO permitir que organizadores administren videojuegos.** Videojuegos es catálogo global:
  crear/editar/borrar queda reservado para `admin`. Los organizadores solo seleccionan
  videojuegos existentes al crear torneos.
- [ ] **NO permitir partidas ni premios de equipo sin inscripción.** Una partida solo puede
  registrarse entre equipos inscritos en el torneo. Si un premio tiene `equipoId`, ese equipo debe
  estar inscrito. Esto evita torneos con `Partidas (1)` y `Inscritos (0)`.
- [ ] **NO volver a poner botones de mutación en páginas públicas.** El sitio `(public)` sigue
  100% read-only (decisión de PR #4). Toda mutación vive en el workspace del rol dueño.
- [ ] **NO agregar endpoints "list-all" al backend para jugadores/partidas/videojuegos.** Modelo
  query-first (Chebotko) a propósito. Las vistas "Todos"/"Recientes" y el overall del panel se
  componen en el **cliente** con `useQueries` (fan-out). Es la solución, no un parche.
- [ ] **NO tocar el latch del modal de videojuegos.** `VideojuegoModal` mantiene su propio estado
  `display` (con `useEffect`) para que el encabezado no parpadee a "Nuevo videojuego" durante la
  animación de cierre del modo edición. Si lo quitás, vuelve el flash.
- [ ] **NO reescribir la auth/RBAC del backend.** Intacta. Las nuevas rutas (PUT/DELETE orgs y
  juegos, GET/DELETE usuarios) usan los mismos `[Authorize(Roles=...)]` y validación por servicio.
- [ ] **NO asumir `node`/`npm` local.** Por seguridad no hay runtime fuera de Docker; el
  type-check real es el `next build` dentro del build del contenedor frontend.
- [ ] **NO cambiar los chips de país de `/jugadores` (Q2) a nombres completos.** El dato es
  **ISO-2** (`KR`, `US`, `BR`). Consultar por nombre completo devuelve vacío.

---

## Estado ejecutivo

Stack completo levanta con un solo `docker compose up --build`. El workspace admin (`/panel`)
ahora tiene CRUD completo y sin bugs: organizadores y videojuegos se pueden listar, crear, editar
y borrar (con bloqueo 409 si tienen torneos); los usuarios se listan, registran y eliminan; el
overview muestra KPIs reales (equipos, organizadores, torneos, partidas) y un bar chart de torneos
por juego. El backend ganó los endpoints que faltaban (PUT/DELETE de orgs y juegos en
`tournaments`; GET/DELETE de usuarios en `auth`). Sitio público sigue read-only, pero el home ahora
consume un showcase visual de una partida T1 vs Gen.G simulada desde `matches`. En la rama
`codex/restrict-videogames-and-match-enrollment`, la suite queda en 143 tests declarados y pasando:
guardas para videojuegos admin-only, partidas/premios solo con equipos inscritos y showcase live.

---

## Estado verificado

Última verificación antes de este handoff:

```text
git branch --show-current:
codex/restrict-videogames-and-match-enrollment

git log --oneline -n 6 (base main ya mergeado con PR #6):
1cefc5c test: add clean integration test runner
bf02d08 docs: update authorization and handoff context
6c546b4 fix(frontend): remove organizer game catalog access
a5db54c fix(auth): enforce tournament participant rules
1cffff3 feat(admin): complete CRUD for organizers, games and users (#6)
c90593e docs(handoff): update for public/private surfaces and browse views (#5)

Verificacion ejecutada en esta rama:
docker compose build matches seeder frontend tests
./scripts/test-clean.sh

Resultado de tests:
Total tests: 143
Passed: 143

Al terminar `./scripts/test-clean.sh`, el script restauró el stack demo limpio:
frontend Started, seeder Exited (0), servicios reconstruidos desde cero.

Casos nuevos clave:
- premio a equipo no inscrito => 409
- partida entre equipos no inscritos => 409
- organizador/fan no pueden crear videojuegos => 403
- `GET /api/partidas/en-vivo/destacada?elapsedSeconds=0` => T1 vs Gen.G 0-0
- `GET /api/partidas/en-vivo/destacada?elapsedSeconds=300` => dragon de T1
- `RIFT-LIVE26` tiene T1 y Gen.G inscritos, pero no partidas historicas ni premios
```

> Nota: `./scripts/test-clean.sh` hace `down -v`, corre la suite y vuelve a levantar una demo limpia.
> Los UUID del seeder cambian en cada cold-boot; pedirlos a la API, no hardcodear.

---

## Qué se hizo en la sesión del PR #6 (admin CRUD)

Backend (cada servicio dueño valida JWT + ownership; errores RFC 7807):

- **tournaments**: `PUT`/`DELETE` para organizadores y videojuegos. Editar/borrar cuando la
  entidad **todavía tiene torneos** devuelve `409 ProblemDetails` (block-on-dependents). Se
  introdujo `MutacionResultado` para llevar el resultado de dominio del service al controller
  (éxito / no-encontrado / bloqueado) sin `throw` crudo.
- **auth**: `GET /api/auth/usuarios` (admin) y `DELETE /api/auth/usuarios/{username}`. Guarda
  contra self-delete y contra borrar el admin bootstrap (409).

Frontend (workspace admin bajo `/panel`; las mutaciones viven acá, nunca en el sitio público):

- **`panel/organizadores/page.tsx`** — lista con diálogos de editar/borrar; se quitó el link roto
  que renderizaba "not found"; el detalle del 409 (bloqueo por torneos) se muestra en un toast.
- **`panel/videojuegos/page.tsx`** — muestra el catálogo completo primero y luego filtra por chips
  de género; crear/editar en un **modal compartido** con `Select` de género; el modal mantiene su
  contenido latcheado (`display` + `useEffect`) para que el encabezado no parpadee a "Nuevo" al
  cerrar el modo edición.
- **`panel/usuarios/page.tsx`** — lista real de usuarios con badges de rol y stats; alta como modal
  (register) y baja (la fila propia está deshabilitada).
- **`panel/page.tsx`** (overview) — KPIs reales (equipos, organizadores, torneos, partidas) y un
  bar chart sin dependencias de torneos por juego; el overview de organizador rellena
  partidas/premios vía fan-out.
- **`panel/equipos/page.tsx`** — se evitó que la fila navegara fuera del panel (el "leak" del sidebar).

Tests: `AdminCrudTests.cs` (nuevo, 16 declaraciones) cubre los ciclos CRUD de org/juego/usuario y
las rutas 401/403/404/409; `GatewayFixture.cs` ganó helpers de auth admin.

---

## Dataset del seeder (crítico)

Conteos esperados tras agregar Gen.G y `RIFT-LIVE26`:

```text
Equipos: 41
Jugadores (suma de integrantes): 202
Videojuegos: 5
Organizadores: 7
Torneos: 13
```

Videojuegos y su género (confirmado en /videojuegos):

```text
Dota 2            → MOBA
League of Legends → MOBA
Valorant          → FPS
Counter-Strike 2  → FPS
Rocket League     → SPORTS
```

Hechos no obvios (siguen vigentes):
- **País se guarda como ISO-2** (`KR`, `US`, `BR`, `UA`, `CN`, `DE`, `FR`, `DK`…). Q2 espera el código.
- **T1 tiene exactamente 3 jugadores explícitos**: Faker (MID), Gumayusi (ADC), Zeus (TOP).
- **Gen.G existe como equipo LoL (`GEN`)** para el showcase live del home: Kiin, Canyon, Chovy,
  Ruler y Duro.
- **RIFT-LIVE26** es un torneo showcase de Riot Games con T1 y Gen.G inscritos. No genera partidas
  históricas ni premios automaticos; la partida en vivo se expone por `matches` como estado
  efimero (`GET /api/partidas/en-vivo/destacada`) para no alterar rankings/counters.
- **UUID se regeneran en cada cold-boot** (`down -v`). No hardcodear IDs.
- Organizadores (nombre exacto en Cassandra): `LoL Esports`, `PGL`, `Riot Games`,
  `BLAST Premier`, `VALORANT Champions Tour`, `ESL FACEIT Group`, `UNIVALLE Esports`.
- **Block-on-dependents**: un organizador o videojuego con torneos asociados NO se puede borrar ni
  editar (409). Para probar el camino feliz de delete, crear uno nuevo sin torneos y borrarlo.

Usuarios demo (password fija, del seeder):

```text
admin            / admin-dev-password
org_<code>       / OrgDemo2024   (ej: org_riot, org_esl, org_vct)
cap_<tag>        / CapDemo2024   (ej: cap_t1, cap_navi, cap_drg, cap_g2)
fan_demo         / FanDemo2024
```

---

## Archivos tocados en la sesión del PR #6

Backend — tournaments:
- `services/tournaments/Esports.Tournaments.Api/Controllers/OrganizadoresController.cs` — PUT/DELETE.
- `services/tournaments/Esports.Tournaments.Api/Controllers/VideojuegosController.cs` — PUT/DELETE.
- `services/tournaments/Esports.Tournaments.Api/Services/OrganizadorService.cs` — lógica + 409.
- `services/tournaments/Esports.Tournaments.Api/Services/VideojuegoService.cs` — lógica + 409.
- `services/tournaments/Esports.Tournaments.Api/Services/MutacionResultado.cs` — nuevo (resultado de dominio).
- `services/tournaments/Esports.Tournaments.Api/Repositories/OrganizadorRepository.cs` — update/delete + conteo de torneos.
- `services/tournaments/Esports.Tournaments.Api/Repositories/VideojuegoRepository.cs` — update/delete + conteo.
- `services/tournaments/Esports.Tournaments.Api/Dtos/Dtos.cs` — DTOs de update.

Backend — auth:
- `services/auth/Esports.Auth.Api/Controllers/AuthController.cs` — GET/DELETE usuarios.
- `services/auth/Esports.Auth.Api/Dtos/AuthDtos.cs` — DTO de listado.
- `services/auth/Esports.Auth.Api/Repositories/IUsuarioRepository.cs` — firmas list/delete.
- `services/auth/Esports.Auth.Api/Repositories/UsuarioRepository.cs` — implementación.

Frontend (workspace admin):
- `frontend/src/app/panel/organizadores/page.tsx` — edit/delete + toast 409.
- `frontend/src/app/panel/videojuegos/page.tsx` — show-all + filtro + modal compartido + latch.
- `frontend/src/app/panel/usuarios/page.tsx` — lista + alta-modal + baja.
- `frontend/src/app/panel/page.tsx` — KPIs reales + bar chart + fan-out org.
- `frontend/src/app/panel/equipos/page.tsx` — fix del leak de navegación.
- `frontend/src/lib/api/auth.ts` — clientes list/delete usuarios.
- `frontend/src/lib/api/torneos.ts` — clientes update/delete org y juego.

Infra/tests:
- `tests/Esports.Gateway.Tests/AdminCrudTests.cs` — nuevo (16 declaraciones).
- `tests/Esports.Gateway.Tests/GatewayFixture.cs` — helpers de auth admin.
- `.gitignore` — ignora `frontend/next-env.d.ts` (artefacto generado por Next).

---

## Commits — ya en `origin/main`

```text
1cffff3 feat(admin): complete CRUD for organizers, games and users (#6)
```

PR #6 mergeado por **squash**; la rama de feature fue borrada. `main` local quedó sincronizada con
`origin/main` (fast-forward `c90593e..1cffff3`) al inicio de esta sesión de cierre.

---

## Decisiones tomadas (con justificación)

- **Block-on-dependents (409) en delete/edit de org y juego.** Razón: borrar un organizador o
  videojuego con torneos vivos dejaría las tablas desnormalizadas (`torneos_por_organizador`,
  `torneos_por_videojuego`) apuntando a un padre inexistente. Mejor rechazar con un 409 claro.
- **`MutacionResultado` en vez de excepciones para el flujo de dominio.** Razón: el controller
  mapea {ok, no-encontrado, bloqueado} a {200/204, 404, 409} con `ProblemDetails`, sin `throw`
  crudo hacia el cliente (regla de oro del proyecto).
- **Guardas de self-delete y admin-bootstrap en `auth`.** Razón: un admin no debe poder dejar el
  sistema sin admin ni borrarse en caliente. Devuelve 409.
- **Latch del modal de videojuegos (`display` + `useEffect`).** Razón: al cerrar el modo edición,
  el prop `juego` pasa a `null` y el encabezado parpadeaba a "Nuevo videojuego" durante la
  animación. El latch conserva el contenido hasta que el modal termina de cerrarse.
- **Show-all-then-filter en videojuegos.** Razón: arrancar con un buscador vacío era confuso; se
  muestra el catálogo completo (fan-out por género) y los chips filtran en memoria.
- **KPIs y bar chart del overview compuestos en cliente (fan-out), sin endpoints nuevos.** Razón:
  modelo query-first; agregar list-all rompería Chebotko.
- **Merge por squash a `main`.** Razón: la rama tenía commits intermedios; squash deja la historia
  de `main` con un commit lógico por PR.

---

## Warnings conocidos (no bloqueantes)

```text
- SYSLIB0060: Rfc2898DeriveBytes constructor deprecated en PasswordService (deuda técnica).
- NU1903: Newtonsoft.Json 9.0.1 advisory (transitiva de xunit, no controlable).
- xUnit analyzer: algunos Assert.True deberían ser Assert.Contains.
- El seeder corre en cada `up` y omite registros ya existentes ("ya existe (omitido)") — idempotente.
- Micro-flash cosmético en el confirm-dialog de borrado (nombre/@ vacío durante la animación de
  cierre). Decidido NO arreglar para evitar churn; es solo visual y no afecta el flujo.
```

---

## Siguiente fase — acción concreta

> El proyecto está funcionalmente completo según el Definition of Done. Lo que sigue es
> opcional / a pedido del usuario.

### Qué hacer (si el usuario lo pide)
1. (Opcional) **CRUD de equipos en el panel admin.** Es lo único de CRUD que quedó diferido:
   crear/editar/borrar equipo requiere denormalización cross-service event-driven (el equipo vive
   en `teams` pero se referencia en inscripciones de `tournaments`). Diseñar el flujo de eventos
   antes de tocar nada — no es un CRUD trivial de una tabla.
2. (Opcional) Verificar por UI los flujos felices de mutación en cada workspace tras un cold-boot:
   capitán inscribe/agrega jugador (201), organizador asigna premio/registra partida en
   `/panel/torneos/[id]` propio (201), admin en cualquiera (201).
3. (Opcional) Aplicar el patrón "default amable" a `/torneos` y `/organizadores` públicos si el
   usuario reporta que arrancan vacíos (no verificado).
4. (Opcional) Accesibilidad fina (focus por teclado, `prefers-reduced-motion`) en las vistas nuevas.

### Qué NO tocar
- Backend `auth`/RBAC, seeder: estables y mergeados.
- El bloqueo 409 block-on-dependents y las guardas de usuarios.
- El modelo query-first: no agregar endpoints list-all.
- El sistema de diseño HUD y la decisión de layout por rol.

### Preguntas abiertas
- Ninguna bloqueante. El CRUD de equipos es la única pieza grande pendiente y depende de si el
  usuario lo quiere para la entrega.

---

## Cómo retomar en 60 segundos

```bash
cd /Users/lukesito/dev/src/github.com/lukehowland/esports-platform
git status --short --branch          # esperado: rama codex/restrict-videogames-and-match-enrollment
git log --oneline --decorate -n 8    # HEAD debe incluir 1cffff3 y commits de integridad
docker compose up --build -d
docker compose ps --all              # todos healthy, seeder Exited (0)
docker compose logs --tail=30 seeder
```

Estado deseado al retomar:
- Rama `codex/restrict-videogames-and-match-enrollment`, basada en `main` con PR #6 mergeado.
- Stack arriba y healthy, seeder `Exited (0)`.
- Frontend en `http://localhost:3000`: público read-only con vistas "Todos"/"Recientes" y showcase
  live T1 vs Gen.G; workspace admin (`/panel`) con CRUD completo de organizadores/videojuegos/usuarios.
- Backend auth/RBAC listo y testeado; suite limpia `143/143` pasando con `./scripts/test-clean.sh`.
