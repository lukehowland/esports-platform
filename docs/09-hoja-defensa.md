# Hoja de Defensa — RF-01 a RF-11 y Q1–Q24

> Guía paso a paso para recorrer cada requerimiento funcional y cada query durante la defensa.
> Cada sección indica: qué pide el requerimiento, dónde se demuestra en backend y frontend, y qué decir.

---

## Preparación antes de la defensa

```bash
# Levantar todo desde cero (esquemas + seeder + servicios)
docker compose down -v
docker compose up --build -d
# Esperar ~30s a que Cassandra levante y los servicios estén healthy
docker compose ps   # verificar que todo dice "healthy"
```

URLs:

| Recurso | URL |
|---|---|
| Frontend | http://localhost:3000 |
| Gateway (API) | http://localhost:8080 |
| Swagger teams | http://localhost:5001/swagger |
| Swagger tournaments | http://localhost:5002/swagger |
| Swagger matches | http://localhost:5003/swagger |
| Swagger ranking | http://localhost:5004/swagger |
| Swagger auth | http://localhost:5005/swagger |
| RabbitMQ | http://localhost:15672 (guest/guest) |

Usuarios demo:

| Usuario | Contraseña | Rol |
|---|---|---|
| admin | admin-dev-password | Administrador |
| org_riot | OrgDemo2024 | Organizador (Riot Games) |
| cap_t1 | CapDemo2024 | Capitán (T1) |
| fan_demo | FanDemo2024 | Fan (solo lectura) |

---

## REQUERIMIENTOS FUNCIONALES

---

### RF-01 — Registrar y gestionar jugadores

**Qué pide:** registrar jugadores con nombre, nickname, email, teléfono y país de origen.

**Demostración backend (Swagger o curl):**

```bash
# Buscar jugador por nickname — muestra TODOS los campos
curl http://localhost:8080/api/jugadores/por-nickname/Faker
# → nombre, nickname, email, telefono, pais, codigo

# Detalle completo por ID
curl http://localhost:8080/api/jugadores/{jugadorId}
# → incluye email y telefono
```

**Demostración frontend:**
1. Ir a **Jugadores** → ver lista con código (J-XXX), nickname, nombre, rol, país
2. Click en cualquier jugador → detalle muestra **email** y **teléfono**
3. Historial de equipos con membresía activa

**Demostración de gestión (admin):**
1. Login como `admin` → Panel → Equipos → click en un equipo
2. Agregar jugador con formulario (nombre, nickname, email, teléfono, país, rol)
3. Editar jugador (lápiz) → modifica contacto
4. Eliminar jugador con equipo activo → **409** (protegido)
5. Liberar jugador primero, luego eliminar → **200** (agente libre)

**Qué decir:**
> "El jugador tiene todos los atributos del MER: nombre, nickname, email, teléfono y país. El código J-001 es legible e inmutable, asignado automáticamente. Un jugador con equipo activo no se puede eliminar — primero se libera."

---

### RF-02 — Registrar y gestionar equipos

**Qué pide:** registrar equipos con nombre, tag identificador y fecha de creación.

**Demostración backend:**

```bash
# Listar equipos por fecha (Q4)
curl http://localhost:8080/api/equipos/por-fecha
# → nombre, tag, pais, fecha_creacion

# Buscar por tag (Q5)
curl http://localhost:8080/api/equipos/por-tag/T1

# Intentar editar equipo con roster → 409
curl -X PUT http://localhost:8080/api/equipos/{id} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Test","tag":"TST","pais":"US"}'

# Intentar eliminar equipo con roster → 409
curl -X DELETE http://localhost:8080/api/equipos/{id} \
  -H "Authorization: Bearer {token}"
```

**Demostración frontend:**
1. **Panel → Equipos** → formulario "Nuevo Equipo" con nombre, tag, país
2. Click en equipo → botones **Editar** y **Eliminar**
3. Intentar eliminar equipo con roster → muestra error 409

**Qué decir:**
> "El CRUD de equipos es exclusivo de admin. No se puede editar ni eliminar un equipo que tiene jugadores activos — hay que vaciar el roster primero. Esto protege la integridad referencial sin necesidad de JOINs, que no existen en Cassandra."

---

### RF-03 — Asignar jugadores a equipos (N:N)

**Qué pide:** asignar uno o más jugadores a uno o más equipos (relación N:N).

**Demostración backend:**

```bash
# Membresías de un jugador (historial temporal)
curl http://localhost:8080/api/jugadores/{id}/membresias
# → lista con equipo, fecha_desde, fecha_hasta (null = activa)

# Código legible inmutable
curl http://localhost:8080/api/jugadores/por-codigo/J-001
```

**Demostración frontend:**
1. Click en cualquier jugador → sección **"Historial de Equipos"**
   - Membresía actual marcada como **ACTUAL**
   - Membresías pasadas con fecha_hasta
2. **Panel → Equipos → [equipo]** → Gestión de Roster:
   - **Liberar** jugador (cierra membresía activa)
   - **Fichar agente libre** (buscador por código o nickname)
   - **Transferir** jugador entre equipos (admin)

**Qué decir:**
> "La relación es N:N temporal: un jugador puede pertenecer a varios equipos a lo largo del tiempo, pero solo a uno de forma activa. Esto se implementa con la tabla `membresias_por_jugador` donde la clustering key es `fecha_desde`. El código J-001 es inmutable y se genera con una tabla de secuencias en Cassandra."

---

### RF-04 — Registrar videojuegos

**Qué pide:** registrar videojuegos con nombre, género y plataforma.

**Demostración backend:**

```bash
# Videojuegos por género (Q8)
curl http://localhost:8080/api/videojuegos/por-genero/MOBA
# → nombre, genero, plataforma (ej: "PC")

curl http://localhost:8080/api/videojuegos/por-genero/FPS
# → Counter-Strike 2 y Valorant, ambos con plataforma "PC"
```

**Demostración frontend:**
1. **Videojuegos** (público) → cada juego muestra nombre, **plataforma** y género
2. **Panel → Videojuegos** → CRUD con formulario nombre + género + plataforma
3. Intentar eliminar videojuego con torneos → **409**

**Qué decir:**
> "Los tres atributos del MER están presentes: nombre, género y plataforma. La plataforma se persiste tanto en la tabla base como en `videojuegos_por_genero`. Los videojuegos con torneos activos no se pueden eliminar."

---

### RF-05 — Registrar organizadores

**Qué pide:** registrar organizadores con nombre y correo electrónico.

**Demostración backend:**

```bash
# Lista de organizadores (Q10)
curl http://localhost:8080/api/organizadores
# → nombre y email de cada organizador
```

**Demostración frontend:**
1. **Organizadores** (público) → nombre y email visibles
2. **Panel → Organizadores** → formulario con nombre y **email**, editar/eliminar
3. Eliminar organizador con torneos → **409**

**Qué decir:**
> "Cada organizador tiene nombre y email como pide el MER. El email se persiste en la tabla base y en `organizadores_lista`. Los organizadores con torneos activos están protegidos contra eliminación."

---

### RF-06 — Crear torneos

**Qué pide:** crear torneos con código, nombre, fechas y videojuego, vinculado a un organizador.

**Demostración backend:**

```bash
# Torneo por código (Q15)
curl http://localhost:8080/api/torneos/por-codigo/UNI-INV26
# → nombre, codigo, fecha_inicio, fecha_fin, videojuego, organizador

# Validación: fecha_fin antes de fecha_inicio → 400
curl -X PUT http://localhost:8080/api/torneos/{id} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Test","fechaFin":"2020-01-01T00:00:00Z"}'
# → 400: "La fecha de fin no puede ser anterior a la fecha de inicio"

# Torneo con inscritos no se puede editar/eliminar → 409
```

**Demostración frontend:**
1. **Torneos** (público) → lista con nombre, videojuego, fecha
2. Click en torneo → detalle con código, rango **fecha inicio → fecha fin**, videojuego, organizador
3. Tabs: Equipos inscritos, Partidas, Premios
4. **Panel → Crear Torneo** → formulario con nombre, código, fecha inicio, **fecha fin**, videojuego, organizador
5. **Panel → Torneos → [torneo]** → Editar/Eliminar con protección 409

**Qué decir:**
> "El torneo tiene todos los atributos del MER más fecha de fin. La validación de fechas se hace en el backend — no solo en el frontend. Los torneos con inscripciones, partidas o premios están protegidos contra edición y eliminación. El código del torneo es una llave de negocio estable."

---

### RF-07 — Inscribir equipos en torneos (N:N)

**Qué pide:** inscribir uno o más equipos en un torneo (relación N:N).

**Demostración backend:**

```bash
# Equipos inscritos en un torneo (Q13)
curl http://localhost:8080/api/torneos/{torneoId}/equipos
# → lista de equipos con fecha de inscripción

# Torneos de un equipo (Q14)
curl http://localhost:8080/api/torneos/por-equipo/{equipoId}

# Inscribir (requiere admin o capitán del equipo)
curl -X POST http://localhost:8080/api/torneos/{torneoId}/inscripciones \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"equipoId":"uuid"}'
```

**Demostración frontend:**
1. Detalle de torneo → tab **Inscritos** con equipos y fecha de inscripción
2. **Panel → Torneos → [torneo]** → botón **"Inscribir equipo"**

**Qué decir:**
> "La inscripción es N:N: un equipo puede estar en varios torneos y un torneo tiene varios equipos. La inscripción publica un evento `TeamRegisteredToTournament` que el servicio de ranking consume para actualizar los counters Q7 y Q23. El capitán solo puede inscribir a su propio equipo."

---

### RF-08 — Registrar partidas

**Qué pide:** registrar partidas dentro de un torneo, incluyendo fecha y resultado.

**Demostración backend:**

```bash
# Partidas de un torneo (Q16)
curl http://localhost:8080/api/partidas/por-torneo/{torneoId}
# → fecha, equipo_local, equipo_visitante, resultado

# Registrar partida (publica evento MatchPlayed)
curl -X POST http://localhost:8080/api/partidas \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"torneoId":"uuid","fecha":"2026-07-02T20:00:00Z","equipoLocalId":"uuid","equipoVisitanteId":"uuid","equipoGanadorId":"uuid","resultado":"2-1",...}'
```

**Demostración frontend:**
1. **Partidas** (público) → tabla con torneo, local, visitante, resultado, fecha
2. **Panel → Torneos → [torneo]** → botón **"Registrar partida"**

**Qué decir:**
> "Una partida registra fecha, participantes, resultado y ganador. Al crear una partida se escribe en 5 tablas en un BATCH (Q16-Q19) y se publica un evento `MatchPlayed` que el ranking consume para actualizar victorias (Q22) y stats (Q24). Los dos equipos deben estar inscritos en el torneo."

---

### RF-09 — Registrar premios

**Qué pide:** registrar premios por monto y tipo, otorgados por un torneo.

**Demostración backend:**

```bash
# Premios de un torneo (Q20)
curl http://localhost:8080/api/torneos/{torneoId}/premios
# → tipo, monto, equipo (opcional)

# Premios de un equipo (Q21)
curl http://localhost:8080/api/premios/por-equipo/{equipoId}
```

**Demostración frontend:**
1. Detalle de torneo → tab **Premios** con monto, tipo y equipo ganador
2. **Panel → Torneos → [torneo]** → botón **"Asignar premio"**

**Qué decir:**
> "Los premios tienen monto y tipo como pide el MER. Opcionalmente se vinculan a un equipo ganador inscrito. Se persisten en `premios_por_torneo` (Q20) y `premios_por_equipo` (Q21) en un BATCH."

---

### RF-10 — Consultar clasificaciones y estadísticas

**Qué pide:** consultar clasificaciones, resultados de partidas y estadísticas de torneos.

**Demostración backend:**

```bash
# Q7: ranking global por torneos
curl "http://localhost:8080/api/ranking/equipos?top=10"
# → posicion, equipoId, totalTorneos

# Q22: ranking por victorias
curl "http://localhost:8080/api/ranking/victorias?top=10"
# → posicion, equipoId, totalVictorias

# Q23: jugadores más activos
curl "http://localhost:8080/api/ranking/jugadores?top=10"
# → posicion, jugadorId, totalTorneos, nombreJugador

# Q24: stats de un equipo en un torneo
curl "http://localhost:8080/api/stats/equipo/{equipoId}/torneo/{torneoId}"
# → victorias, derrotas, partidasJugadas
```

**Demostración frontend:**
1. **Rankings** → tabs: Equipos (Q7), Victorias (Q22), Jugadores (Q23)
2. Selector Top 5/10/20
3. Nombres de equipos resueltos vía REST al servicio teams

**Qué decir:**
> "Los rankings usan tablas counter en Cassandra. No son INSERT — solo UPDATE atómicos. Se actualizan por eventos: una inscripción incrementa Q7 y Q23, una partida incrementa Q22 y Q24. Hay consistencia eventual de milisegundos, que es el comportamiento esperado de un sistema event-driven. El top-N se ordena en el servicio porque los counters no pueden ser clustering keys."

---

### RF-11 — Autenticación para funciones de administración

**Qué pide:** requerir autenticación para acceso a funciones de administración.

**Demostración backend:**

```bash
# Login
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin-dev-password"}'
# → token JWT, rol, nombre

# Credenciales inválidas → 401
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"wrong"}'

# Cualquier mutación sin token → 401
curl -X DELETE http://localhost:8080/api/equipos/{id}
# → 401

# Capitán intentando borrar (solo admin puede) → 403
```

**Demostración frontend:**
1. Sin login → solo se ve sitio público (lectura)
2. Login como **admin** → panel completo con todos los CRUD
3. Login como **organizador** → solo puede gestionar sus propios torneos
4. Login como **capitán** → solo puede gestionar su equipo y roster

**Qué decir:**
> "La autenticación usa JWT emitido por el servicio auth y persistido en Cassandra. Cada servicio dueño de mutaciones valida el token y el ownership — no confiamos en el gateway ni en ocultar botones en el frontend. Los roles son admin, organizador, capitán y fan, cada uno con permisos distintos según la tabla de autorización documentada en el contrato API."

---

## QUERIES Q1–Q24

> Para cada query: endpoint, tabla Cassandra, servicio, y cómo demostrarlo.

---

### Servicio teams (Q1–Q6)

| # | Descripción | Endpoint | Tabla | Demo rápida |
|---|---|---|---|---|
| **Q1** | Jugador por nickname | `GET /api/jugadores/por-nickname/Faker` | `jugadores_por_nickname` | Devuelve datos completos del jugador |
| **Q2** | Jugadores por país | `GET /api/jugadores/por-pais/KR` | `jugadores_por_pais` | 24 jugadores de Corea |
| **Q3** | Jugadores de equipo por país | `GET /api/equipos/{id}/jugadores?pais=KR` | `jugadores_por_equipo` | 3 jugadores coreanos de T1 |
| **Q4** | Equipos por fecha | `GET /api/equipos/por-fecha` | `equipos_por_fecha` | 41 equipos, más reciente primero |
| **Q5** | Equipo por tag | `GET /api/equipos/por-tag/T1` | `equipos_por_tag` | Lookup exacto por partition key |
| **Q6** | Integrantes de equipo | `GET /api/equipos/{id}/integrantes` | `integrantes_por_equipo` | Faker, Gumayusi, Zeus |

**Qué decir sobre el modelo:**
> "Cada query tiene su propia tabla desnormalizada — es la metodología Chebotko. No hay JOINs. La partition key de Q2 es `pais`, la de Q3 es `equipo_id` con clustering por `pais`. Q4 usa un bucket sintético GLOBAL para agrupar todos los equipos en una sola partición, ordenados por fecha descendente."

---

### Servicio tournaments (Q8–Q15, Q20–Q21)

| # | Descripción | Endpoint | Tabla | Demo rápida |
|---|---|---|---|---|
| **Q8** | Videojuegos por género | `GET /api/videojuegos/por-genero/MOBA` | `videojuegos_por_genero` | 2 juegos MOBA con plataforma |
| **Q9** | Torneos de un videojuego | `GET /api/videojuegos/{id}/torneos` | `torneos_por_videojuego` | 4 torneos de LoL |
| **Q10** | Todos los organizadores | `GET /api/organizadores` | `organizadores_lista` | 7 organizadores con email |
| **Q11** | Torneos de un organizador | `GET /api/organizadores/{id}/torneos` | `torneos_por_organizador` | Torneos ordenados por fecha desc |
| **Q12** | Torneos por fecha | `GET /api/torneos/por-fecha` | `torneos_por_fecha` | 13 torneos, más reciente primero |
| **Q13** | Equipos inscritos en torneo | `GET /api/torneos/{id}/equipos` | `equipos_por_torneo` | 8 equipos en UNI-INV26 |
| **Q14** | Torneos de un equipo | `GET /api/torneos/por-equipo/{id}` | `torneos_por_equipo` | 4 torneos de T1 |
| **Q15** | Torneo por código | `GET /api/torneos/por-codigo/UNI-INV26` | `torneo_por_codigo` | Lookup exacto por código |
| **Q20** | Premios de un torneo | `GET /api/torneos/{id}/premios` | `premios_por_torneo` | Ordenados por monto desc |
| **Q21** | Premios de un equipo | `GET /api/premios/por-equipo/{id}` | `premios_por_equipo` | 2 premios de T1 |

**Qué decir sobre el modelo:**
> "Tournaments es el servicio más grande porque aloja videojuegos, organizadores, torneos, inscripciones y premios — todos existen en función del torneo. Q13 y Q14 son la relación N:N equipo-torneo modelada como dos tablas: una particionada por torneo y otra por equipo. Los premios también tienen doble tabla para consultar por torneo (Q20) o por equipo (Q21)."

---

### Servicio matches (Q16–Q19)

| # | Descripción | Endpoint | Tabla | Demo rápida |
|---|---|---|---|---|
| **Q16** | Partidas de un torneo | `GET /api/partidas/por-torneo/{id}` | `partidas_por_torneo` | Cronológico, más reciente primero |
| **Q17** | Historial de un equipo | `GET /api/partidas/por-equipo/{id}` | `partidas_por_equipo` | 13 partidas de T1 (doble escritura) |
| **Q18** | Partidas por fecha | `GET /api/partidas/por-fecha/2026-09-12` | `partidas_por_fecha` | 4 partidas ese día |
| **Q19** | Enfrentamientos directos | `GET /api/partidas/entre/{id}/{rivalId}` | `partidas_por_rivales` | 5 enfrentamientos T1 vs G2 |

**Qué decir sobre los fixes:**
> "Aplicamos tres correcciones al modelo original:
> 1. **Q18**: la partition key original era timestamp exacto — cada partida quedaba sola. Fix: particionar por `dia` (tipo date), agrupando todas las partidas de una jornada.
> 2. **Q19**: la tabla original solo encontraba partidas donde el equipo fue local. Fix: escritura bidireccional — dos filas por partida para encontrar enfrentamientos sin importar la localía.
> 3. **Q17**: doble escritura como Q19, una fila por cada equipo participante."

---

### Servicio ranking (Q7, Q22–Q24)

| # | Descripción | Endpoint | Tabla | Demo rápida |
|---|---|---|---|---|
| **Q7** | Ranking por torneos | `GET /api/ranking/equipos?top=10` | `ranking_equipos_global` | Top con posición y totalTorneos |
| **Q22** | Ranking por victorias | `GET /api/ranking/victorias?top=10` | `ranking_victorias` | Top con posición y totalVictorias |
| **Q23** | Jugadores más activos | `GET /api/ranking/jugadores?top=10` | `ranking_jugadores_activos` | Top con nombre y totalTorneos |
| **Q24** | Stats equipo en torneo | `GET /api/stats/equipo/{id}/torneo/{id}` | `stats_equipo_por_torneo` | victorias, derrotas, partidasJugadas |

**Qué decir sobre el fix de counters:**
> "El modelo original ponía la métrica como clustering key para tener orden en disco. Problema: en Cassandra las primary keys son inmutables — no se puede hacer UPDATE sobre una clustering column. Fix: sacamos la métrica de la clustering key y la hacemos columna counter. El ranking se incrementa atómicamente con `UPDATE tabla SET col = col + 1`. El top-N se ordena en el servicio, lo cual es instantáneo a este volumen."

**Qué decir sobre la comunicación entre servicios:**
> "Ranking no tiene POST públicos. Se actualiza solo consumiendo eventos de RabbitMQ via MassTransit:
> - `TeamRegisteredToTournament` → incrementa Q7 (torneos del equipo) y Q23 (torneos de cada jugador del roster)
> - `MatchPlayed` → incrementa Q22 (victorias del ganador) y Q24 (stats de ambos equipos en el torneo)"

---

## Puntos clave para preguntas del ingeniero

### "¿Por qué no usan SQL?"
> "El proyecto es sobre Apache Cassandra y modelado query-first (Chebotko). No hay JOINs, no hay transacciones distribuidas. Cada query tiene su propia tabla desnormalizada y la consistencia entre copias se mantiene con BATCH dentro del mismo servicio."

### "¿Por qué tantas tablas?"
> "24 queries = 24 tablas + 6 tablas base + 3 tablas auxiliares (código, membresías, secuencias). Es el costo del modelado query-first: optimizamos lectura a costa de escritura. A cambio, cada consulta es un simple `WHERE` sobre la partition key, sin JOINs ni full scans."

### "¿Cómo se comunican los servicios?"
> "Lecturas síncronas: REST vía HttpClient tipado (ej: tournaments llama a teams para obtener el nombre del equipo al inscribir). Eventos asíncronos: MassTransit sobre RabbitMQ (ej: inscripción publica evento que ranking consume). Ningún servicio lee el keyspace de otro — regla dura."

### "¿Qué pasa si RabbitMQ se cae?"
> "Los rankings dejan de actualizarse hasta que vuelva. Los datos transaccionales (inscripciones, partidas) no se pierden porque viven en Cassandra. Los rankings son read-models derivados con consistencia eventual — es el trade-off esperado."

### "¿Está listo para producción?"
> "No. Es un entorno académico: un solo nodo Cassandra (RF=1, sin tolerancia a fallos), sin HTTPS, sin HA. A escala real se agregarían: múltiples nodos con RF=3, buckets temporales en las particiones GLOBAL, idempotencia en los consumidores de eventos, y HTTPS en el gateway."

### "¿Por qué el gateway no valida auth?"
> "Por diseño: el gateway solo proxyea y reenvía el header Authorization. La validación de JWT, roles y ownership la hace cada servicio dueño de la mutación. Esto evita un punto único de fallo en la autorización y permite que cada servicio defina sus propias reglas de negocio."

### "¿Qué son los 409?"
> "HTTP 409 Conflict. Los usamos cuando una operación es válida sintácticamente pero viola una regla de negocio: eliminar un equipo con roster, eliminar un jugador con membresía activa, editar un torneo con inscripciones, borrar un videojuego con torneos. Es un ProblemDetails (RFC 7807) con el detalle del conflicto."

---

## Flujo de demostración sugerido (20-30 min)

1. **Arquitectura** (2 min): mostrar `docker compose ps`, explicar los 5 servicios + gateway + Cassandra + RabbitMQ
2. **Home con showcase en vivo** (1 min): T1 vs Gen.G simulado en tiempo real
3. **Sitio público** (5 min): recorrer Equipos, Jugadores, Videojuegos, Organizadores, Torneos, Partidas, Rankings
4. **Login admin** (1 min): demostrar autenticación
5. **CRUD admin** (5 min): crear equipo, agregar jugador, gestión de roster, crear torneo
6. **Protecciones 409** (3 min): intentar eliminar equipo con roster, torneo con inscritos, videojuego con torneos
7. **Roles** (3 min): login como organizador (solo sus torneos), capitán (solo su equipo), fan (solo lectura)
8. **Queries Chebotko** (5 min): recorrer Q1-Q24 vía Swagger mostrando que cada query mapea a una tabla
9. **Eventos** (2 min): mostrar RabbitMQ management, explicar flujo inscripción → ranking
10. **Preguntas** (5 min)

---

## Checklist rápido pre-defensa

- [ ] `docker compose ps` — 9 contenedores healthy
- [ ] Frontend carga en http://localhost:3000
- [ ] Login admin funciona
- [ ] Swagger de cada servicio accesible
- [ ] RabbitMQ management accesible
- [ ] Datos del seeder visibles (41 equipos, 13 torneos, 201 jugadores)
- [ ] Suite de tests pasa: `docker compose run --rm tests` → 169/169
