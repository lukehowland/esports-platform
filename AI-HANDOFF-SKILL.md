# AI-HANDOFF-SKILL — Protocolo de transferencia de contexto entre IAs

> **Propósito**: Este archivo define el protocolo para que Claude y Codex puedan intercambiar contexto
> sin perder estado cuando una sesión llega a su límite. Se usa junto con `Handoff.md`.
>
> **Cómo usarlo**: Al inicio de una sesión nueva, leer este archivo primero, luego `Handoff.md`.
> Al cerrar una sesión, seguir la sección EXPORT para generar un `Handoff.md` actualizado.

---

## IMPORT — Cómo retomar desde Handoff.md

Seguir estos pasos **en orden** al inicio de cada sesión nueva:

### Paso 1 — Leer el estado declarado

```bash
cat Handoff.md
```

Identificar en el handoff:
- Qué fase o tarea dice que está completa.
- Qué dice que **NO** hacer a continuación.
- Qué archivos fueron modificados.
- Cuántos tests pasaron (número exacto).

### Paso 2 — Verificar el estado real del repo

```bash
git status --short --branch
git log --oneline --decorate -n 12
git diff --stat HEAD
```

Si hay cambios sin commitear o la rama no coincide con lo que dice el handoff, **parar y preguntar al usuario** antes de continuar.

### Paso 3 — Verificar el stack (si está corriendo)

```bash
docker compose ps --all
docker compose logs --tail=20 seeder
```

Estado esperado: todos los servicios `healthy`, `seeder` en `Exited (0)`.
Si el stack no está arriba, no significa que el trabajo esté incompleto — el usuario puede haberlo bajado.

### Paso 4 — Confirmar el conteo de tests

El número de tests que pasaron está documentado en `Handoff.md`. Antes de correr tests, leer el handoff para saber si el número es el esperado.

Para correr tests (tarda ~3 min, requiere stack arriba):

```bash
docker compose run --rm --no-deps tests
```

### Paso 5 — Leer la sección "Siguiente fase"

`Handoff.md` siempre termina con una sección que dice exactamente qué hacer a continuación.
No inventar trabajo. Si la sección no existe o es ambigua, preguntar al usuario.

### Paso 6 — Confirmar con el usuario

Antes de escribir código, resumir en 3–5 líneas:
- Qué se hizo hasta ahora.
- Qué dice el handoff que sigue.
- Qué vas a hacer en esta sesión.

Esperar confirmación.

---

## EXPORT — Cómo generar un Handoff.md de calidad

Usar esta plantilla al cerrar una sesión. Completar cada sección con información real (no estimaciones).

### Plantilla obligatoria

```markdown
# Handoff — Esports Platform <nombre-fase>

Fecha: <YYYY-MM-DD>
Repo: /Users/lukesito/dev/src/github.com/lukehowland/esports-platform
Rama de trabajo: <rama>
Generado por: <Claude / Codex>

---

## ⚠️ ESTADO FINAL — QUÉ NO HACER AL RETOMAR

> Esta sección es la más importante. Un agente nuevo tiende a reimplementar
> trabajo ya hecho si no ve esta lista explícita.

- [ ] NO reimplementar <fase X> — ya está completa y commiteada.
- [ ] NO reescribir los tests de <archivo> — ya fueron reconciliados con el seeder actual.
- [ ] NO cambiar <cosa Y> — fue una decisión deliberada documentada abajo.
- [ ] NO tocar `frontend/` en esta tanda (acordado con el usuario).

---

## Estado ejecutivo

<2–4 líneas: qué funciona, cuál es el estado global, qué falta.>

---

## Estado verificado

Última verificación antes de este handoff:

```text
git log --oneline -n 6:
<pegar salida real>

docker compose ps --all:
<pegar salida real o escribir "no verificado en Docker">

Tests: <N>/<N> pasando
```

---

## Dataset del seeder (crítico para tests)

> Los tests dependen de datos específicos que el seeder carga. Documentar aquí
> cualquier hecho no obvio sobre los datos — si no está documentado, el próximo
> agente lo va a asumir distinto y los tests van a fallar.

### Equipos con jugadores explícitos

| Tag | Jugadores explícitos | Nicknames clave |
|-----|---------------------|-----------------|
| <TAG> | <N jugadores> | <nick1, nick2> |

### Torneos y códigos

| Código | Nombre completo | Organizador | Videojuego | Equipos |
|--------|----------------|-------------|------------|---------|
| <CÓDIGO> | <nombre> | <org> | <juego> | <N> |

### Organizadores (nombres exactos como están en Cassandra)

```text
<lista exacta, no abreviada — el fixture busca por nombre exacto>
```

### Conteos del seeder (última ejecución)

```text
<pegar los últimos logs del seeder que muestran totales>
```

---

## Tests — estado y reconciliación

Total: **<N>/<N> pasando**

Desglose por archivo:

| Archivo | Tests | Notas |
|---------|-------|-------|
| TeamsTests.cs | <N> | <reconciliado contra seeder: DRX→NAVI, etc.> |
| TournamentsTests.cs | <N> | <...> |
| MatchesTests.cs | <N> | <...> |
| RankingTests.cs | <N> | <...> |
| AuthTests.cs | <N> | <nuevo en esta fase> |

Cambios no obvios en los tests (por qué no usan los valores originales):

- `Q3_T1_Con3Jugadores`: T1 tiene exactamente 3 jugadores explícitos (Faker, Gumayusi, Zeus).
  Sin esta info, el próximo agente esperaría 5 (default del seeder).
- `Q9_Valorant` en lugar de `Q9_Dota2`: Dota 2 no tiene torneos en el seeder actual.
- `Q11_BLAST` en lugar de `Q11_PGL`: PGL organiza 0 torneos en el seeder actual.
- `Q21_FurId` (FURIA-LoL) como "equipo sin premios": G2 SÍ tiene premios (subcampeón).
- `Q17_NAVI`: tiene VICTORIA y DERROTA en IEM-COL26 (1W 3L). No asumir solo victorias.

---

## Archivos modificados en esta sesión

<Lista de archivos tocados, agrupados por categoría. No poner solo "varios archivos".>

Auth/shared:
- `shared/Esports.Auth.Shared/...`

Servicios:
- `services/teams/.../EquiposController.cs` — proteger POST con [Authorize]
- ...

Infra/tests:
- `docker-compose.yml` — servicio auth, JWT env vars
- `tests/.../GatewayFixture.cs` — AdminToken, AuthedPost, AdminPost, AdminPostJson
- `tests/.../AuthTests.cs` — nuevo, <N> tests
- ...

---

## Commits de esta sesión

```text
<git log --oneline desde el punto de bifurcación>
```

---

## Decisiones tomadas (con justificación)

> Documenta las decisiones no obvias. Sin esto, el próximo agente las revierte.

- **Validación JWT distribuida (no en gateway)**: cada servicio valida su propio token.
  Razón: zero-trust + autonomía de microservicio + el gateway es proxy puro (YARP).
- **`matches` verifica dueño del torneo vía `TournamentsClient` (REST)**: no cross-keyspace.
  Razón: regla dura del proyecto — un keyspace por servicio.
- **`[property:]` prefix NO usado en DataAnnotations**: rompe en .NET 10 records → 500.
- **`!!` en YAML**: el secret JWT fue single-quoted (`'...'`) para evitar que YAML lo parsee.

---

## Warnings conocidos (no bloqueantes)

```text
<Lista de advertencias del build o tests que NO hay que arreglar ahora.>
```

- SYSLIB0060: `Rfc2898DeriveBytes` constructor deprecated → migrar a `.Pbkdf2` (deuda técnica)
- NU1903: Newtonsoft.Json 9.0.1 advisory (transitiva de xunit, no controlable)
- xUnit analyzer: algunos `Assert.True` deberían ser `Assert.Contains`

---

## Siguiente fase — acción concreta

> Esta sección dice exactamente qué hacer en la próxima sesión.
> Si es ambigua, no sirve.

### Qué hacer

1. <Primer paso concreto, con archivo y ruta si aplica>
2. <Segundo paso>
3. ...

### Qué NO tocar

- <Archivos o áreas fuera de scope en la próxima sesión>

### Preguntas abiertas para el usuario

- <Solo si hay algo bloqueante que el agente no puede decidir solo>

---

## Cómo retomar en 60 segundos

```bash
cd /Users/lukesito/dev/src/github.com/lukehowland/esports-platform
git status --short --branch
git log --oneline --decorate -n 8
docker compose ps --all
docker compose logs --tail=30 seeder
```

Estado deseado al retomar:
- Rama: `<rama>`
- Stack: todos healthy, seeder `Exited (0)`
- Tests: <N>/<N> (correr solo si el stack está arriba)
- Trabajo pendiente: ver sección "Siguiente fase"
```

---

## Reglas del protocolo

### Para el agente que GENERA el handoff (cierre de sesión)

1. **Nunca usar estimaciones.** Si no corriste los tests, escribe "no verificado". Si no viste los logs del seeder, escríbelo.
2. **La sección "QUÉ NO HACER" es obligatoria.** Si no la llenas, el siguiente agente va a reimplementar trabajo.
3. **Documentar el dataset.** Cualquier nombre de equipo, tag, código de torneo, nickname de jugador, o nombre de organizador que un test use: escribirlo en el handoff. Los tests fallan si el agente siguiente asume distintos valores.
4. **Hacer commit del handoff actualizado.** `docs(handoff): update for <fase>`.
5. **No mentir sobre el estado.** Si algo no funcionó, documentarlo.

### Para el agente que RECIBE el handoff (inicio de sesión)

1. **Leer primero "QUÉ NO HACER".** No saltar a implementar.
2. **Verificar con `git status` antes de cualquier acción.** El repo puede estar en un estado distinto al descrito.
3. **Confirmar con el usuario** antes de escribir código.
4. **No extender el scope** más allá de lo que dice la sección "Siguiente fase" sin consultar.
5. **Si el handoff contradice el estado real del repo, el repo gana.** El handoff puede estar desactualizado.

---

## Errores frecuentes que este protocolo previene

| Error | Por qué ocurre | Cómo lo previene este protocolo |
|-------|---------------|----------------------------------|
| Reimplementar trabajo ya hecho | El session summary dice "F5 estaba por comenzar" pero Codex ya lo terminó | Sección "QUÉ NO HACER" + verificar `git log` |
| Tests que fallan por datos distintos | El agente asume "ESL Gaming" pero en Cassandra está "ESL FACEIT Group" | Sección "Dataset del seeder" con nombres exactos |
| Asumir equipo sin premios incorrecto | G2 es subcampeón, tiene premios; FURIA-LoL no tiene | Documentar en "Cambios no obvios en tests" |
| Reescribir GatewayFixture desde cero | No hay indicación de que ya fue reconciliado | Nota explícita en sección de tests |
| YAML `!!` rompe el compose | `!!` es type-cast en YAML; secret con `!` falla | Documentado en "Decisiones tomadas" |
| Número de jugadores por equipo incorrecto | T1 tiene 3 explícitos, no 5 (default) | Tabla "Equipos con jugadores explícitos" |

---

## Historial de versiones de este protocolo

| Fecha | Cambio | Motivo |
|-------|--------|--------|
| 2026-06-16 | v1.0 — versión inicial | Codex completó F0–F8, primer intercambio Claude↔Codex perdió contexto sobre tests reconciliados y dataset del seeder |
