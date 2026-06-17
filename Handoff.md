# Handoff — Esports Platform: separación de superficies + browse público

Fecha: 2026-06-17
Repo: `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`
Rama de trabajo: `main` (PR #4 ya mergeado por squash)
Generado por: Claude

Este handoff reemplaza al anterior (rediseño HUD + auth real, ya mergeado). Documenta el
estado tras **separar el sitio público de los workspaces por rol** y **hacer que el catálogo
público muestre datos por defecto** (vistas "Todos"/"Recientes") en vez de buscadores vacíos.

---

## ⚠️ ESTADO FINAL — QUÉ NO HACER AL RETOMAR

- [ ] **NO reimplementar la separación de superficies.** El split `(public)` (read-only) vs
  workspaces privados por rol (`/panel` con sidebar, `/mi-equipo` cockpit) ya está completo y
  mergeado (PR #4, squash `592b32f` en `origin/main`).
- [ ] **NO volver a poner botones de mutación en páginas públicas.** Es deliberado: el sitio
  público es 100% lectura. Las mutaciones viven en el workspace del rol dueño
  (`/mi-equipo`, `/panel/torneos/[id]`, etc.) con ownership implícito.
- [ ] **NO agregar endpoints "list-all" al backend para jugadores/partidas/videojuegos.** El
  backend es query-first (Chebotko) a propósito. Las vistas "Todos"/"Recientes" se componen
  en el **cliente** con `useQueries` (fan-out). Es la solución, no un parche.
- [ ] **NO reescribir la auth/RBAC del backend.** Intacta, testeada (122/122), mergeada.
- [ ] **NO cambiar los chips de país de `/jugadores` (tab Q2) a nombres completos.** El dato es
  **ISO-2** (`KR`, `US`, `BR`). Consultar por nombre completo devuelve vacío.
- [ ] **NO usar `.nombre`/`.codigo` sobre `getTorneosPorOrganizador`/`getTorneosPorFecha`/
  `getTorneosPorEquipo`.** Devuelven `TorneoResumenResponse`/`TorneoPorEquipoResponse` con
  `nombreTorneo`/`nombreVideojuego`. Solo `getTorneoPorId` y `getTorneoPorCodigo` traen `nombre`/`codigo`.
- [ ] **NO asumir `node`/`npm` local.** Por seguridad no hay runtime fuera de Docker; el
  type-check real es el `pnpm build` dentro del build del contenedor frontend.

---

## Estado ejecutivo

Stack completo levanta con un solo `docker compose up --build`. El frontend ahora tiene dos
superficies limpias: público read-only (navbar) y workspaces privados por rol (sin navbar
público). El catálogo público abre con contenido por defecto: 197 jugadores, 5 videojuegos con
su género real, y las 40 partidas más recientes — sin exigir buscar primero. Verificado en
navegador como anónimo/fan, sin errores de consola. Backend auth/RBAC sin cambios.

---

## Estado verificado

Última verificación antes de este handoff:

```text
git log --oneline -n 4 (main):
592b32f refactor(frontend): separate public/private surfaces and add default browse views (#4)
5f71b12 Merge pull request #3 from lukehowland/fix/frontend-audit
39eb58e docs(handoff): document frontend redesign and audit state
1858ca8 fix(frontend): query players by ISO-2 country code (Q2)

git status: ## main (limpio, sin cambios sin commitear)

docker compose ps --all:
auth, cassandra, frontend, gateway, matches, rabbitmq, ranking, teams, tournaments → healthy
seeder → Exited (0)

Frontend build (Docker, Next strict type-check): exit 0 (sin errores)
Auditoría visual en navegador (anónimo/fan):
  /jugadores  → tab "Todos" = 197 jugadores, búsqueda + chips de rol; filtro CONTROLLER = 16. OK
  /videojuegos→ chip "TODOS" = 5 juegos con género real (MOBA/FPS/SPORTS). OK
  /partidas   → tab "Recientes" = últimas 40 partidas ordenadas por fecha desc. OK
  /equipos    → chevron de afordancia; clic → /equipos/[id] lista el roster (5 integrantes). OK
Console errors: ninguno.

Tests de integración backend: 122/122 (NO re-ejecutados esta sesión; cero cambios de backend).
```

> Nota: esta sesión no hizo `down -v`, así que los UUID del seeder NO se regeneraron. Tras un
> cold-boot (`down -v` + `up`) los IDs cambian — pedirlos a la API, no hardcodear.

---

## Qué se hizo en esta sesión (solo frontend)

Vistas browse por defecto (público), todas compuestas en el cliente con `useQueries`:
- **`(public)/jugadores/page.tsx`** — nueva pestaña **"Todos"** (default): fan-out de
  `getIntegrantesPorEquipo` sobre `getEquiposPorFecha`, dedupe por `jugadorId`, búsqueda libre
  (nickname/nombre/país/rol) + chips de rol derivados del dato + tag de equipo por jugador.
  Pestañas Q1 (nickname) y Q2 (país) conservadas.
- **`(public)/videojuegos/page.tsx`** — chip **"TODOS"** (default): fan-out de
  `getVideojuegosPorGenero` sobre los 7 géneros, cada juego etiquetado con su **género real**
  (no uno hardcodeado); los chips de género filtran en memoria.
- **`(public)/partidas/page.tsx`** — nueva pestaña **"Recientes"** (default): fan-out de
  `getPartidasPorTorneo` sobre `getTorneosPorFecha`, flatten + sort por fecha desc, top 40.
  Pestañas Q18 (por fecha) y Q19 (cara a cara) conservadas.
- **`(public)/equipos/page.tsx`** — chevron `›` en cada fila (afordancia de clic). El detalle
  `/equipos/[id]` ya funcionaba: lista integrantes (Q6), filtro por país (Q3), torneos (Q14),
  partidas (Q17), premios (Q21), stats (Q24). El reporte de "no muestra detalle" era un glitch
  de timing del clic, no un bug real.

(La separación de superficies — route groups `(public)`, layouts por rol, mover mutaciones a
workspaces — está en los commits previos de la misma rama, todos dentro del squash `592b32f`.)

---

## Dataset del seeder (crítico)

Conteos confirmados en navegador esta sesión:

```text
Equipos: 40
Jugadores (suma de integrantes): 197
Videojuegos: 5
Organizadores: 7
Torneos: 12
```

Videojuegos y su género (confirmado en /videojuegos):

```text
Dota 2            → MOBA
League of Legends → MOBA
Valorant          → FPS
Counter-Strike 2  → FPS
Rocket League     → SPORTS
```

Hechos no obvios (siguen vigentes del handoff anterior):
- **País se guarda como ISO-2** (`KR`, `US`, `BR`, `UA`, `CN`, `DE`, `FR`, `DK`…). Q2 espera el código.
- **T1 tiene exactamente 3 jugadores explícitos**: Faker (MID), Gumayusi (ADC), Zeus (TOP).
- **UUID se regeneran en cada cold-boot** (`down -v`). No hardcodear IDs.
- Organizadores (nombre exacto en Cassandra): `LoL Esports`, `PGL`, `Riot Games`,
  `BLAST Premier`, `VALORANT Champions Tour`, `ESL FACEIT Group`, `UNIVALLE Esports`.

Usuarios demo (password fija, del seeder):

```text
admin            / admin-dev-password
org_<code>       / OrgDemo2024   (ej: org_riot, org_esl, org_vct)
cap_<tag>        / CapDemo2024   (ej: cap_t1, cap_navi, cap_drg, cap_g2)
fan_demo         / FanDemo2024
```

---

## Archivos tocados en esta sesión

Frontend (vistas browse por defecto):
- `frontend/src/app/(public)/jugadores/page.tsx` — tab "Todos" (fan-out integrantes).
- `frontend/src/app/(public)/videojuegos/page.tsx` — chip "TODOS" (fan-out por género).
- `frontend/src/app/(public)/partidas/page.tsx` — tab "Recientes" (fan-out por torneo).
- `frontend/src/app/(public)/equipos/page.tsx` — chevron de afordancia.

(El grueso de la separación de superficies está en commits previos del mismo PR #4.)

---

## Commits — ya en `origin/main`

```text
592b32f refactor(frontend): separate public/private surfaces and add default browse views (#4)
```

PR #4 mergeado por **squash** y rama `feat/frontend-surfaces` borrada (local + remota).
El detalle (6 commits originales: checkpoint, route-group split, read-only, mover mutaciones,
fixup del import del manual, browse views) quedó colapsado en ese único commit.

---

## Decisiones tomadas (con justificación)

- **Vistas "Todos"/"Recientes" compuestas en cliente (`useQueries` fan-out), NO nuevos endpoints.**
  Razón: el backend es query-first (24 tablas desnormalizadas para Q1–Q24); agregar un list-all
  rompería el modelo Chebotko. El fan-out respeta el contrato existente y es barato (≤40 queries).
- **Se conservan las pestañas Q1/Q2/Q18/Q19** junto a las vistas "Todos"/"Recientes".
  Razón: valor académico (mapean 1:1 a las consultas del examen); "Todos" es solo el default amable.
- **Género real por juego** vía fan-out de los 7 géneros (no etiqueta fija). Razón: el endpoint
  `getVideojuegosPorGenero` no devuelve el género en el body; se infiere de qué query lo trajo.
- **Merge por squash a `main`.** Razón: la rama tenía un commit "checkpoint" y un "fixup"; squash
  deja la historia de `main` limpia (un PR = un commit lógico).
- **El bug #4 (detalle de equipo) NO era un bug.** La relación equipo→jugadores existe y el
  detalle funciona; solo faltaba afordancia visual (chevron). No reabrir como bug.

---

## Warnings conocidos (no bloqueantes)

```text
- SYSLIB0060: Rfc2898DeriveBytes constructor deprecated en PasswordService (deuda técnica).
- NU1903: Newtonsoft.Json 9.0.1 advisory (transitiva de xunit, no controlable).
- xUnit analyzer: algunos Assert.True deberían ser Assert.Contains.
- El seeder corre en cada `up` y omite registros ya existentes ("ya existe (omitido)") — es idempotente.
```

---

## Siguiente fase — acción concreta

### Qué hacer (si el usuario lo pide)
1. (Opcional) Aplicar el mismo patrón "default amable" a `/torneos` y `/organizadores` si el
   usuario reporta que también arrancan vacíos (no verificado esta sesión).
2. (Opcional) Verificar por UI los flujos felices de mutación post-refactor en cada workspace:
   capitán inscribe/agrega jugador (201), organizador asigna premio/registra partida en
   `/panel/torneos/[id]` propio (201), admin en cualquiera (201).
3. (Opcional) Accesibilidad fina (focus por teclado, `prefers-reduced-motion`) en las vistas nuevas.

### Qué NO tocar
- Backend `auth`/RBAC, tests, seeder: estables y mergeados.
- El sistema de diseño HUD y la decisión de layout por rol.
- El modelo query-first: no agregar endpoints list-all.

### Preguntas abiertas
- Ninguna bloqueante.

---

## Cómo retomar en 60 segundos

```bash
cd /Users/lukesito/dev/src/github.com/lukehowland/esports-platform
git status --short --branch          # esperado: ## main, limpio
git log --oneline --decorate -n 8
docker compose up --build -d
docker compose ps --all              # todos healthy, seeder Exited (0)
docker compose logs --tail=30 seeder
```

Estado deseado al retomar:
- Rama `main`, sincronizada con `origin/main`.
- Stack arriba y healthy, seeder `Exited (0)`.
- Frontend en `http://localhost:3000`: público con vistas "Todos"/"Recientes", workspaces por rol.
- Backend auth/RBAC listo y testeado (122/122).
