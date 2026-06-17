# Handoff — Esports Platform: frontend redesign + auth real

Fecha: 2026-06-16
Repo: `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`
Rama de trabajo: `main` (los fixes de esta sesión se integran por PR `fix/frontend-audit`)
Generado por: Claude

Este handoff reemplaza al anterior (que cubría la fase de backend `auth-service`, ya
mergeada a `main`). Ahora documenta el estado tras completar el **rediseño completo del
frontend con auth JWT real, interfaz por rol y diseño "Broadcast HUD"**, más una auditoría
visual de toda la app.

---

## ⚠️ ESTADO FINAL — QUÉ NO HACER AL RETOMAR

- [ ] **NO reimplementar el frontend.** El rediseño HUD + auth real ya está completo, auditado y commiteado (`acf07ec` en `origin/main` + fixes en esta tanda).
- [ ] **NO reescribir la auth del backend.** El microservicio `auth` + RBAC por servicio ya está hecho, testeado (122/122) y mergeado a `main`. Está intacto.
- [ ] **NO cambiar los chips de país de `/jugadores` a nombres completos.** El dato guarda país como **ISO-2** (`KR`, `US`, `BR`). Consultar por nombre completo devuelve vacío. Los chips usan código ISO-2 con etiqueta legible a propósito.
- [ ] **NO usar `.nombre`/`.codigo` sobre objetos de `getTorneosPorOrganizador`, `getTorneosPorFecha` o `getTorneosPorEquipo`.** Esos endpoints devuelven `TorneoResumenResponse`/`TorneoPorEquipoResponse` con `nombreTorneo`/`nombreVideojuego`. Solo `TorneoResponse` (de `getTorneoPorId`) y `TorneoPorCodigoResponse` tienen `nombre`/`codigo`.
- [ ] **NO asumir que `npm`/`node` están disponibles fuera de Docker.** Por seguridad, no hay runtime local; todo va por Docker. (El type-check se puede correr solo con un binario de node ya cacheado, en modo lectura.)

---

## Estado ejecutivo

Stack completo funcionando con un solo `docker compose up --build`. Cold-boot desde cero
(`down -v` + `up --build`) verificado: los 9 contenedores quedan `healthy`, el seeder corre
solo, autentica como admin, puebla datos + ranking por eventos, registra usuarios demo por
rol y termina `Exited (0)`. **No hace falta ningún paso secundario manual.** El frontend
rediseñado (HUD de transmisión, violeta+lima, Rajdhani) está vivo en `http://localhost:3000`
con login JWT real e interfaz diferenciada por rol. Auditoría visual completa: todas las
páginas públicas + los 4 flujos de rol funcionan, sin errores de consola.

---

## Estado verificado

Última verificación antes de este handoff (cold-boot real desde cero):

```text
docker compose down -v --remove-orphans   → OK (borra volúmenes, simula primera vez)
docker compose up --build -d              → exit 0

docker compose ps --all:
auth          healthy   5005
cassandra     healthy   9042
frontend      healthy   3000
gateway       healthy   8080
matches       healthy   5003
rabbitmq      healthy   5672 / 15672
ranking       healthy   5004
teams         healthy   5001
tournaments   healthy   5002
seeder        Exited (0)

Frontend type-check (tsc --noEmit): exit 0 (sin errores)
Smoke tests gateway: organizadores/torneos OK, login admin OK (token 279 chars),
  /api/auth/me OK, mutación sin token → 401.
Auditoría visual: home, login, jugadores, rankings (Q7+Q23), manual, torneos,
  torneos/[id] (Equipos/Partidas/Premios), equipos, videojuegos, organizadores,
  partidas, panel admin, panel organizador, cockpit capitán, home fan → todas OK.
Console errors: ninguno.

Tests de integración backend: 122/122 (no re-ejecutados en esta sesión; sin cambios
  de backend salvo Q23, ya cubierto y mergeado).
```

---

## Qué se hizo en esta fase (frontend)

Auth real de punta a punta:
- `localStorage` key `esports-token`; `fetcher` adjunta `Authorization: Bearer`.
- `login()` real contra `/api/auth/login`; `me()` hidrata identidad al cargar; `logout()` limpia.
- Login con usuario/contraseña + accesos rápidos demo (admin/organizador/capitán/fan).
- `RequireRole` protege rutas; redirect según rol.

Interfaz diferenciada por rol (el sidebar NO es universal — decisión de diseño):
- **admin → sidebar** backoffice (equipos, organizadores, videojuegos, torneos, **usuarios**).
- **organizador → sidebar** propio (mis torneos filtrados por `organizadorId`, crear torneo, videojuegos).
- **capitán → cockpit single-column** (su único equipo, tabs roster/agregar/torneos). Sin rail a propósito.
- **fan → experiencia pública** con navbar (solo lectura), sin panel.

Diseño "Broadcast HUD":
- Paleta violeta `#7C3AED` + lima `#C2FF3D` sobre `void #0A0A0F`.
- Tipografía Rajdhani (display) + Inter (body) + JetBrains Mono (datos/eyebrows).
- Paneles angulares (`hud-clip`), `StatTile`, scoreboard como elemento firma de la home.

Bugs cerrados (todos verificados en la auditoría):
- **Jugadores en blanco** → tabs Q1 (nickname) / Q2 (país) con chips ISO-2 y datos por defecto.
- **Ranking Q23 con IDs** → resuelve nicknames (backend: evento `TeamRegisteredToTournament` lleva `JugadorRef(Id, Nickname)`; ranking guarda `ranking_jugadores_meta`; frontend muestra nombre).
- **Manual frágil** → import del `.md` como raw string bundleado + `prose`; ya no lee de `process.cwd()`.
- **Radix `<SelectItem value="">`** → centinela `"__none__"` → `undefined`.
- **Chips de país con nombre completo** (encontrado en esta auditoría) → ahora ISO-2.

---

## Dataset del seeder (crítico)

Conteos de la última corrida (cold-boot de esta sesión):

```text
Equipos: 40
Torneos: 12
Organizadores: 7
Ranking equipos por torneos: 40
Ranking equipos por victorias: 40
Ranking jugadores activos: 50
```

Hechos no obvios:
- **País se guarda como ISO-2** (`KR`, `US`, `BR`, `UA`, `CN`, `DE`, `FR`, `DK`…), no nombre completo. Q2 (`/api/jugadores/por-pais/{pais}`) espera el código.
  - Conteos reales por país: `KR=19, CN=30, BR=21, US=16, DE=9, DK=9, FR=5, UA=4`. `AR` y `CO` = 0.
- **T1 tiene exactamente 3 jugadores explícitos**: Faker (MID), Gumayusi (ADC), Zeus (TOP). No 5.
- **Los UUID se regeneran en cada cold-boot** (`down -v`). No hardcodear IDs de torneo/equipo en pruebas manuales; pedirlos a la API (`/api/torneos/por-fecha`).
- Organizadores (nombre exacto en Cassandra): `LoL Esports`, `PGL`, `Riot Games`, `BLAST Premier`, `VALORANT Champions Tour`, `ESL FACEIT Group`, `UNIVALLE Esports`.

Usuarios demo (password fija, públicas del seeder):

```text
admin                 / admin-dev-password
org_<code>            / OrgDemo2024   (ej: org_riot → Riot Games, org_esl, org_vct)
cap_<tag>             / CapDemo2024   (ej: cap_t1 → T1, cap_navi, cap_g2)
fan_demo              / FanDemo2024
```

---

## Archivos tocados en esta sesión

Frontend (fixes post-rediseño + audit):
- `frontend/src/app/mi-equipo/page.tsx` — usar `nombreTorneo`/`nombreVideojuego` (TorneoPorEquipoResponse).
- `frontend/src/app/panel/page.tsx` — `OrgOverview` usar `nombreTorneo`/`nombreVideojuego` (TorneoResumenResponse).
- `frontend/src/app/jugadores/page.tsx` — chips de país por ISO-2 + default `KR` + uppercase input.

(El grueso del rediseño + auth real está en el commit `acf07ec`, ya en `origin/main`.)

---

## Commits de esta sesión (adelante de origin/main)

```text
1858ca8 fix(frontend): query players by ISO-2 country code (Q2)
b521706 fix(frontend): use TorneoResumenResponse fields in org overview
de6440a fix(frontend): use correct TorneoPorEquipoResponse field names in mi-equipo
```

`acf07ec feat(frontend): complete HUD redesign with real JWT auth and role interfaces`
ya está en `origin/main` (mergeado por PR #2).

---

## Decisiones tomadas (con justificación)

- **Sidebar solo para admin y organizador.** Tienen varias áreas de gestión paralelas. Capitán gestiona una sola entidad (cockpit enfocado) y fan no gestiona nada (público). Forzar un rail a esos roles sería decoración, no estructura.
- **Token en `localStorage`** (demo académico): sobrevive refresh y se valida con `/api/auth/me` al cargar. La seguridad real está en el backend (cada servicio valida JWT + ownership).
- **Chips de país por ISO-2**: el dato es ISO-2; mostrar nombre y consultar por código.
- **Q23 con tabla `ranking_jugadores_meta` aparte**: la tabla counter no admite columnas no-counter. El nickname vive en una tabla plana en el mismo keyspace `esports_ranking` (no cross-keyspace).
- **Git por PR, no push directo a `main`.** El push directo está bloqueado por política; los fixes se integran por rama + PR.

---

## Warnings conocidos (no bloqueantes)

```text
- SYSLIB0060: Rfc2898DeriveBytes constructor deprecated en PasswordService (deuda técnica).
- NU1903: Newtonsoft.Json 9.0.1 advisory (transitiva de xunit, no controlable).
- xUnit analyzer: algunos Assert.True deberían ser Assert.Contains.
```

---

## Siguiente fase — acción concreta

### Qué hacer (si el usuario lo pide)
1. Integrar los commits de esta sesión por PR `fix/frontend-audit` → `main` y sincronizar local.
2. (Opcional) Pulir mutaciones felices end-to-end por UI: organizador crea torneo propio (201), capitán agrega jugador a su equipo (201), admin registra usuario (201). El backend ya lo soporta; verificar el feedback de UI (toasts/ProblemDetails) en cada caso.
3. (Opcional) Revisar accesibilidad fina (focus por teclado, `prefers-reduced-motion`) en las páginas nuevas.

### Qué NO tocar
- Backend `auth`/RBAC, tests, seeder: estables y mergeados.
- El sistema de diseño HUD y la decisión de layout por rol.

### Preguntas abiertas
- Ninguna bloqueante.

---

## Cómo retomar en 60 segundos

```bash
cd /Users/lukesito/dev/src/github.com/lukehowland/esports-platform
git status --short --branch
git log --oneline --decorate -n 8
docker compose up --build -d
docker compose ps --all          # todos healthy, seeder Exited (0)
docker compose logs --tail=30 seeder
```

Estado deseado al retomar:
- Rama `main`, sincronizada con `origin/main` (tras mergear el PR de esta sesión).
- Stack arriba y healthy, seeder `Exited (0)`.
- Frontend en `http://localhost:3000` con login JWT real e interfaz por rol.
- Backend auth/RBAC listo y testeado (122/122).
