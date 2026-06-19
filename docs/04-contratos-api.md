# 04 — Contratos de API (REST)

> Endpoints que el frontend consume (vía el gateway) y que los servicios usan entre sí. Definir esto **antes** de implementar permite que el frontend arranque con mocks sin esperar al backend.

## Convención de ruteo (clave con 24 endpoints)

Con tantas queries que cruzan dominios, el ruteo se vuelve un problema si anidamos recursos. La regla que lo resuelve:

> **El primer segmento después de `/api/` decide el servicio, siempre.** Una query que cruza dominios se expone bajo el prefijo del servicio **dueño de la tabla**, nunca anidada bajo otro recurso.

Así el gateway rutea por prefijo, sin ambigüedad ni reglas especiales:

| Prefijo | Servicio |
|---|---|
| `/api/jugadores/**` | teams |
| `/api/equipos/**` | teams |
| `/api/videojuegos/**` | tournaments |
| `/api/organizadores/**` | tournaments |
| `/api/torneos/**` | tournaments |
| `/api/inscripciones/**` | tournaments |
| `/api/premios/**` | tournaments |
| `/api/partidas/**` | matches |
| `/api/ranking/**` | ranking |
| `/api/stats/**` | ranking |
| `/api/auth/**` | auth |

Ejemplo de la regla en acción: "torneos de un equipo" (Q14) NO va a `/api/equipos/{id}/torneos` (eso sería teams), va a `/api/torneos/por-equipo/{equipoId}` (la tabla `torneos_por_equipo` vive en tournaments). "Historial de un equipo" (Q17) va a `/api/partidas/por-equipo/{equipoId}` (matches).

## Códigos de estado

`200` lectura ok · `201` creado · `400` input inválido · `401` sin token/token inválido · `403` rol u ownership incorrecto · `404` no existe · `502/503` dependencia REST caída · errores con cuerpo `ProblemDetails`.

---

## auth — `/api/auth`

### Login
- `POST /api/auth/login` — devuelve JWT y perfil.
  ```jsonc
  { "username": "org_riot", "password": "OrgDemo2024" }
  ```
  Respuesta:
  ```jsonc
  {
    "token": "jwt",
    "rol": "organizador",
    "nombre": "Riot Games",
    "organizadorId": "uuid|null",
    "equipoId": "uuid|null",
    "expiraEn": "2026-06-17T03:14:51Z"
  }
  ```

### Registro demo
- `POST /api/auth/register` — crea usuarios demo; protegido, solo `admin`.
  ```jsonc
  {
    "username": "cap_t1",
    "password": "CapDemo2024",
    "rol": "capitan",
    "equipoId": "uuid",
    "nombreDisplay": "Capitan T1"
  }
  ```
  Valida rol y vinculos:
  - `admin` y `fan` no aceptan `organizadorId` ni `equipoId`.
  - `organizador` requiere `organizadorId` y no acepta `equipoId`.
  - `capitan` requiere `equipoId` y no acepta `organizadorId`.
  - Roles fuera de `admin`, `organizador`, `capitan`, `fan` devuelven `400`.

### Perfil
- `GET /api/auth/me` — requiere token y devuelve claims normalizados:
  ```jsonc
  { "username": "admin", "rol": "admin", "nombre": "Administrador del sistema" }
  ```

### Reglas de autorización

| Mutación | Regla |
|---|---|
| `POST /api/equipos` | `admin` |
| `PUT /api/equipos/{equipoId}` | `admin`; bloquea con `409` si tiene roster activo |
| `DELETE /api/equipos/{equipoId}` | `admin`; bloquea con `409` si tiene roster activo |
| `POST /api/equipos/{equipoId}/jugadores` | `admin` o `capitan` con `equipo_id == equipoId` |
| `PUT /api/jugadores/{jugadorId}` | `admin` o `capitan` del equipo activo del jugador |
| `DELETE /api/jugadores/{jugadorId}` | `admin`; bloquea con `409` si tiene equipo activo |
| `POST /api/jugadores/{jugadorId}/liberar` | `admin` o `capitan` del equipo activo |
| `POST /api/jugadores/{jugadorId}/asignar` | `admin`; o `capitan` del destino si es agente libre |
| `POST /api/videojuegos` | `admin` |
| `PUT /api/videojuegos/{videojuegoId}` | `admin`; bloquea con `409` si tiene torneos |
| `DELETE /api/videojuegos/{videojuegoId}` | `admin`; bloquea con `409` si tiene torneos |
| `POST /api/organizadores` | `admin` |
| `PUT /api/organizadores/{organizadorId}` | `admin`; bloquea con `409` si tiene torneos |
| `DELETE /api/organizadores/{organizadorId}` | `admin`; bloquea con `409` si tiene torneos |
| `POST /api/torneos` | `admin` u `organizador` con `organizador_id == organizadorId` del body |
| `PUT /api/torneos/{torneoId}` | `admin` u `organizador` dueño; bloquea con `409` si tiene inscritos/premios |
| `DELETE /api/torneos/{torneoId}` | `admin` u `organizador` dueño; bloquea con `409` si tiene inscritos/premios |
| `POST /api/torneos/{torneoId}/inscripciones` | `admin` o `capitan` con `equipo_id == equipoId` del body |
| `POST /api/torneos/{torneoId}/premios` | `admin` u `organizador` dueño del torneo; si hay `equipoId`, debe estar inscrito |
| `POST /api/partidas` | `admin` u `organizador` dueño del torneo; ambos equipos deben estar inscritos |

Todas las lecturas Q1–Q24 son públicas/anónimas.

---

## teams — `/api/jugadores`, `/api/equipos`

### Escritura
- `POST /api/equipos` — crear equipo. → `BATCH` `equipos` + `equipos_por_fecha` + `equipos_por_tag`.
  ```jsonc
  { "nombre": "Tigres eSports", "tag": "TIG", "pais": "BO" }
  ```
- `PUT /api/equipos/{equipoId}` — editar nombre, tag y país (solo `admin`, `409` con roster).
- `DELETE /api/equipos/{equipoId}` — eliminar un equipo sin roster (solo `admin`).
- `POST /api/equipos/{equipoId}/jugadores` — agregar jugador a un equipo. → `BATCH` `jugadores` + `jugadores_por_nickname` + `jugadores_por_pais` + `jugadores_por_equipo` + `integrantes_por_equipo`.
  ```jsonc
  {
    "nickname": "ElTigre", "nombre": "Juan Perez", "pais": "BO", "rol": "MID",
    "email": "eltigre@tig.gg", "telefono": "+591-70000000"
  }
  ```
- `PUT /api/jugadores/{jugadorId}` — editar nombre, email y teléfono.
- `DELETE /api/jugadores/{jugadorId}` — eliminar un agente libre (solo `admin`).
- `POST /api/jugadores/{jugadorId}/liberar` — cerrar la membresía activa.
- `POST /api/jugadores/{jugadorId}/asignar` — fichar un agente libre o transferirlo como admin.
  ```jsonc
  { "equipoDestinoId": "uuid", "rol": "MID" }
  ```

### Lectura
- `GET /api/jugadores/por-nickname/{nickname}` — **Q1**. → `jugadores_por_nickname`.
- `GET /api/jugadores/por-pais/{pais}` — **Q2**. → `jugadores_por_pais`.
- `GET /api/equipos/{equipoId}/jugadores?pais={pais}` — **Q3** (pais opcional). → `jugadores_por_equipo`.
- `GET /api/equipos/por-fecha` — **Q4** (más reciente primero). → `equipos_por_fecha`.
- `GET /api/equipos/por-tag/{tag}` — **Q5**. → `equipos_por_tag`.
- `GET /api/equipos/{equipoId}/integrantes` — **Q6**. → `integrantes_por_equipo`.
- `GET /api/equipos/{equipoId}` — equipo por id (lo usa tournaments por REST).
- `GET /api/jugadores/{jugadorId}` — detalle canónico con email/teléfono.
- `GET /api/jugadores/por-codigo/{codigo}` — lookup por código legible `J-001`.
- `GET /api/jugadores/{jugadorId}/membresias` — historial temporal de equipos.

---

## tournaments — `/api/videojuegos`, `/api/organizadores`, `/api/torneos`, `/api/premios`

### Escritura
- `POST /api/videojuegos` — crear videojuego (solo `admin`). → `BATCH` `videojuegos` + `videojuegos_por_genero`.
  ```jsonc
  { "nombre": "League of Legends", "genero": "MOBA", "plataforma": "PC" }
  ```
- `PUT /api/videojuegos/{videojuegoId}` / `DELETE` — gestión admin; `409` si tiene torneos.
- `POST /api/organizadores` — crear organizador. → `BATCH` `organizadores` + `organizadores_lista`.
  ```jsonc
  { "nombre": "Liga Santa Cruz", "email": "contacto@ligasc.gg" }
  ```
- `PUT /api/organizadores/{organizadorId}` / `DELETE` — gestión admin; `409` si tiene torneos.
- `POST /api/torneos` — crear torneo. → `BATCH` `torneos` + `torneos_por_videojuego` + `torneos_por_organizador` + `torneos_por_fecha` + `torneo_por_codigo`.
  ```jsonc
  {
    "nombre": "Copa Santa Cruz 2026", "codigo": "CSC26",
    "videojuegoId": "uuid", "organizadorId": "uuid",
    "fechaInicio": "2026-07-01T18:00:00Z",
    "fechaFin": "2026-07-08T23:00:00Z"
  }
  ```
- `PUT /api/torneos/{torneoId}` — editar nombre y fecha de fin; `409` si tiene inscritos/premios.
  ```jsonc
  { "nombre": "Copa Santa Cruz Finals", "fechaFin": "2026-07-09T23:00:00Z" }
  ```
- `DELETE /api/torneos/{torneoId}` — eliminar torneo sin inscritos/premios.
- `POST /api/torneos/{torneoId}/inscripciones` — inscribir equipo. → REST a teams (nombre + roster), `BATCH` `equipos_por_torneo` + `torneos_por_equipo`, **publica `TeamRegisteredToTournament`**.
  ```jsonc
  { "equipoId": "uuid" }
  ```
- `POST /api/torneos/{torneoId}/premios` — asignar premio (opcionalmente a un equipo ganador inscrito). → `BATCH` `premios_por_torneo` + `premios_por_equipo`.
  ```jsonc
  { "monto": 5000.00, "tipo": "Primer lugar", "equipoId": "uuid|null" }
  ```

### Lectura
- `GET /api/videojuegos/por-genero/{genero}` — **Q8**. → `videojuegos_por_genero`.
- `GET /api/videojuegos/{videojuegoId}/torneos` — **Q9**. → `torneos_por_videojuego`.
- `GET /api/organizadores` — **Q10**. → `organizadores_lista`.
- `GET /api/organizadores/{organizadorId}/torneos` — **Q11**. → `torneos_por_organizador`.
- `GET /api/torneos/por-fecha` — **Q12** (más reciente primero). → `torneos_por_fecha`.
- `GET /api/torneos/{torneoId}/equipos` — **Q13**. → `equipos_por_torneo`.
- `GET /api/torneos/por-equipo/{equipoId}` — **Q14**. → `torneos_por_equipo`.
- `GET /api/torneos/por-codigo/{codigo}` — **Q15**. → `torneo_por_codigo`.
- `GET /api/torneos/{torneoId}/premios` — **Q20** (mayor a menor monto). → `premios_por_torneo`.
- `GET /api/premios/por-equipo/{equipoId}` — **Q21** (mayor a menor monto). → `premios_por_equipo`.
- `GET /api/torneos/{torneoId}` — torneo por id.

---

## matches — `/api/partidas`

### Escritura
- `POST /api/partidas` — registrar partida entre dos equipos inscritos en el torneo. → `BATCH` `partidas` + `partidas_por_torneo` + `partidas_por_equipo` (2 filas) + `partidas_por_fecha` + `partidas_por_rivales` (2 filas), **publica `MatchPlayed`**.
  ```jsonc
  {
    "torneoId": "uuid", "nombreTorneo": "Copa Santa Cruz 2026",
    "fecha": "2026-07-02T20:00:00Z",
    "equipoLocalId": "uuid", "nombreLocal": "Tigres eSports",
    "equipoVisitanteId": "uuid", "nombreVisitante": "Pumas Gaming",
    "equipoGanadorId": "uuid", "resultado": "2-1"
  }
  ```

### Lectura
- `GET /api/partidas/en-vivo/destacada?elapsedSeconds={0..1800}` — showcase público no histórico para el home. Simula T1 vs Gen.G durante 30 minutos, con oro, kills, torres y objetivos. No escribe Cassandra ni publica `MatchPlayed`.
- `GET /api/partidas/por-torneo/{torneoId}` — **Q16** (cronológico). → `partidas_por_torneo`.
- `GET /api/partidas/por-equipo/{equipoId}` — **Q17**. → `partidas_por_equipo`.
- `GET /api/partidas/por-fecha/{dia}` — **Q18** (`dia` = `YYYY-MM-DD`). → `partidas_por_fecha`.
- `GET /api/partidas/entre/{equipoId}/{rivalId}` — **Q19** (enfrentamientos directos, ambos sentidos). → `partidas_por_rivales`.

---

## ranking — `/api/ranking`, `/api/stats` (solo lectura)

> No expone POST/PUT: se actualiza consumiendo eventos.

- `GET /api/ranking/equipos?top={n}` — **Q7** (Top-N por torneos). → `ranking_equipos_global`, ordenado en el servicio.
  ```jsonc
  [ { "posicion": 1, "equipoId": "uuid", "totalTorneos": 12 } ]
  ```
- `GET /api/ranking/victorias?top={n}` — **Q22** (Top-N por victorias). → `ranking_victorias`.
- `GET /api/ranking/jugadores?top={n}` — **Q23** (jugadores más activos). → `ranking_jugadores_activos`.
- `GET /api/stats/equipo/{equipoId}/torneo/{torneoId}` — **Q24**. → `stats_equipo_por_torneo`.
  ```jsonc
  { "equipoId": "uuid", "torneoId": "uuid", "victorias": 4, "derrotas": 1, "partidasJugadas": 5 }
  ```

---

## Swagger

Cada servicio expone Swagger UI en `/swagger` (`:5001`–`:5005`). Para el frontend alcanza con esas URLs + esta lista de endpoints. El gateway puede opcionalmente agregar los Swaggers, pero no es necesario para la demo.

## Resumen rápido: query → endpoint

| Q | Endpoint |
|---|---|
| Q1 | `GET /api/jugadores/por-nickname/{nickname}` |
| Q2 | `GET /api/jugadores/por-pais/{pais}` |
| Q3 | `GET /api/equipos/{id}/jugadores?pais=` |
| Q4 | `GET /api/equipos/por-fecha` |
| Q5 | `GET /api/equipos/por-tag/{tag}` |
| Q6 | `GET /api/equipos/{id}/integrantes` |
| Q7 | `GET /api/ranking/equipos?top=` |
| Q8 | `GET /api/videojuegos/por-genero/{genero}` |
| Q9 | `GET /api/videojuegos/{id}/torneos` |
| Q10 | `GET /api/organizadores` |
| Q11 | `GET /api/organizadores/{id}/torneos` |
| Q12 | `GET /api/torneos/por-fecha` |
| Q13 | `GET /api/torneos/{id}/equipos` |
| Q14 | `GET /api/torneos/por-equipo/{id}` |
| Q15 | `GET /api/torneos/por-codigo/{codigo}` |
| Q16 | `GET /api/partidas/por-torneo/{id}` |
| Q17 | `GET /api/partidas/por-equipo/{id}` |
| Q18 | `GET /api/partidas/por-fecha/{dia}` |
| Q19 | `GET /api/partidas/entre/{id}/{rivalId}` |
| Q20 | `GET /api/torneos/{id}/premios` |
| Q21 | `GET /api/premios/por-equipo/{id}` |
| Q22 | `GET /api/ranking/victorias?top=` |
| Q23 | `GET /api/ranking/jugadores?top=` |
| Q24 | `GET /api/stats/equipo/{id}/torneo/{id}` |
