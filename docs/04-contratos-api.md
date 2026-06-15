# 04 — Contratos de API (REST)

> Estos son los endpoints que el frontend va a consumir (vía el gateway) y que los servicios usan entre sí. Definir esto **antes** de implementar permite que el equipo de frontend arranque con mocks sin esperar al backend.

## Convenciones de ruta

- Todas las rutas empiezan con `/api/`.
- Recursos en plural y minúscula: `/api/torneos`, `/api/equipos`, `/api/partidas`.
- El frontend pega **siempre al gateway** (`http://localhost:8080`). El gateway preserva el path y lo rutea al servicio correcto.

## Ruteo del Gateway (YARP)

| Prefijo de ruta | Servicio destino |
|---|---|
| `/api/equipos/**`, `/api/jugadores/**` | `teams` |
| `/api/torneos/**`, `/api/organizadores/**`, `/api/videojuegos/**`, `/api/inscripciones/**` | `tournaments` |
| `/api/partidas/**` | `matches` |
| `/api/ranking/**` | `ranking` |

> Cuidado con un solapamiento: tanto Teams como Tournaments usan rutas que arrancan con `/api/equipos`. Para evitar choque, las consultas "torneos de un equipo" y "partidas de un equipo" van bajo sus propios prefijos (`/api/torneos/...` no aplica acá), así que se modelan como sub-recursos del lado correcto. Ver tabla por servicio: Q2 se expone como `/api/equipos/{id}/torneos` **en Tournaments**, y Q8 como `/api/equipos/{id}/partidas` **en Matches**. Para que YARP no se confunda, ruteamos por **segundo segmento**: `/api/equipos/{id}/torneos` → tournaments, `/api/equipos/{id}/partidas` → matches, `/api/equipos/**` (resto) → teams. (Detalle de config YARP en `docs/06`.)

## Códigos de estado estándar

- `200 OK` — lectura exitosa.
- `201 Created` — recurso creado (devolver el recurso o su id).
- `400 Bad Request` — input inválido.
- `404 Not Found` — recurso no existe.
- `502/503` — un servicio dependiente (REST) no respondió.
- Errores con cuerpo `ProblemDetails`.

---

## Teams (`/api/equipos`, `/api/jugadores`)

### `POST /api/equipos`
Crea un equipo. → escribe `equipos`.
```jsonc
// Request
{ "nombre": "Tigres eSports", "pais": "Bolivia" }
// Response 201
{ "equipoId": "uuid", "nombre": "Tigres eSports", "pais": "Bolivia", "fechaCreacion": "2026-06-15T..." }
```

### `GET /api/equipos/{equipoId}`
Trae un equipo por id (lo usa Tournaments por REST para el nombre). → lee `equipos`.
```jsonc
// Response 200
{ "equipoId": "uuid", "nombre": "Tigres eSports", "pais": "Bolivia" }
```

### `POST /api/equipos/{equipoId}/jugadores`
Agrega un jugador al equipo. → `BATCH` sobre `jugadores` + `jugadores_por_equipo` + `jugadores_por_pais`.
```jsonc
// Request
{ "nombre": "Juan Perez", "nickname": "ElTigre", "email": "j@x.com", "pais": "Bolivia" }
// Response 201
{ "jugadorId": "uuid", "equipoId": "uuid", "nombre": "Juan Perez", "nickname": "ElTigre", "pais": "Bolivia" }
```

### `GET /api/equipos/{equipoId}/jugadores?pais={pais}` — **Q3**
Jugadores de un equipo, opcionalmente filtrados por país. → lee `jugadores_por_equipo`.
```jsonc
// Response 200
[ { "jugadorId": "uuid", "nombre": "Juan Perez", "nickname": "ElTigre", "pais": "Bolivia" } ]
```

### `GET /api/jugadores/por-pais/{pais}` — **Q10**
Jugadores registrados en un país. → lee `jugadores_por_pais`.
```jsonc
// Response 200
[ { "jugadorId": "uuid", "nombre": "Juan Perez", "nickname": "ElTigre", "nombreEquipo": "Tigres eSports" } ]
```

---

## Tournaments (`/api/torneos`, `/api/organizadores`, `/api/videojuegos`)

### `POST /api/torneos`
Crea un torneo. → `BATCH` sobre `torneos` + `torneos_por_organizador` + `torneos_por_videojuego`.
```jsonc
// Request
{
  "nombre": "Copa Santa Cruz 2026",
  "organizadorId": "uuid", "nombreOrganizador": "Liga SCZ",
  "videojuegoId": "uuid", "nombreVideojuego": "League of Legends",
  "fechaInicio": "2026-07-01T18:00:00Z"
}
// Response 201
{ "torneoId": "uuid", "nombre": "Copa Santa Cruz 2026", ... }
```

### `GET /api/torneos/{torneoId}`
Trae un torneo por id. → lee `torneos`.

### `POST /api/torneos/{torneoId}/premios`
Agrega un premio al torneo. → escribe `premios_por_torneo`.
```jsonc
// Request
{ "monto": 5000.00, "tipo": "Primer lugar" }
// Response 201
{ "premioId": "uuid", "torneoId": "uuid", "monto": 5000.00, "tipo": "Primer lugar" }
```

### `GET /api/torneos/{torneoId}/premios` — **Q6**
Premios de un torneo, mayor a menor monto. → lee `premios_por_torneo`.

### `POST /api/torneos/{torneoId}/inscripciones`
Inscribe un equipo en un torneo. → pide `nombre_equipo` a Teams (REST), hace `BATCH` sobre `equipos_por_torneo` + `torneos_por_equipo`, y **publica `TeamRegisteredToTournament`**.
```jsonc
// Request
{ "equipoId": "uuid" }
// Response 201
{ "torneoId": "uuid", "equipoId": "uuid", "nombreEquipo": "Tigres eSports", "fechaInscripcion": "..." }
```

### `GET /api/torneos/{torneoId}/equipos` — **Q1**
Equipos inscritos en un torneo. → lee `equipos_por_torneo`.

### `GET /api/equipos/{equipoId}/torneos` — **Q2**
Torneos de un equipo, más reciente primero. → lee `torneos_por_equipo`.

### `GET /api/organizadores/{organizadorId}/torneos` — **Q5**
Torneos de un organizador, más reciente primero. → lee `torneos_por_organizador`.

### `GET /api/videojuegos/{videojuegoId}/torneos` — **Q7**
Torneos de un videojuego, por fecha de inicio. → lee `torneos_por_videojuego`.

---

## Matches (`/api/partidas`)

### `POST /api/partidas`
Registra una partida. → `BATCH` sobre `partidas` + `partidas_por_torneo` + **dos filas** en `partidas_por_equipo` (local y visitante). Opcionalmente valida torneo/equipos vía REST.
```jsonc
// Request
{
  "torneoId": "uuid", "nombreTorneo": "Copa Santa Cruz 2026",
  "fecha": "2026-07-02T20:00:00Z",
  "equipoLocalId": "uuid", "nombreEquipoLocal": "Tigres eSports",
  "equipoVisitanteId": "uuid", "nombreEquipoVisitante": "Pumas Gaming",
  "resultado": "2-1"
}
// Response 201
{ "partidaId": "uuid", ... }
```

### `GET /api/torneos/{torneoId}/partidas` — **Q4**
Partidas de un torneo, cronológico (más reciente primero). → lee `partidas_por_torneo`.
> Nota de ruteo: esta ruta empieza con `/api/torneos` pero su recurso final es `partidas`. Va a **Matches**. El gateway la rutea por el patrón `/api/torneos/{id}/partidas`.

### `GET /api/equipos/{equipoId}/partidas` — **Q8**
Historial de partidas de un equipo. → lee `partidas_por_equipo`.

---

## Ranking (`/api/ranking`)

### `GET /api/ranking/global?top={n}` — **Q9**
Top-N de equipos por cantidad de torneos. → lee toda la partición `bucket='GLOBAL'` de `ranking_equipos_global` y ordena en el servicio.
```jsonc
// Response 200
[ { "posicion": 1, "equipoId": "uuid", "totalTorneos": 12 },
  { "posicion": 2, "equipoId": "uuid", "totalTorneos": 9 } ]
```
> El Ranking **no** expone POST/PUT públicos: se actualiza solo, consumiendo eventos.

---

## Swagger

Cada servicio expone Swagger UI en `/swagger`. El gateway puede opcionalmente agregar los Swaggers, pero para la demo alcanza con que el frontend tenga las URLs de Swagger de cada servicio (5001–5004) más la colección de endpoints de arriba.
