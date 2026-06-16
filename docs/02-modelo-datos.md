# 02 — Modelo de Datos (Cassandra / Chebotko)

## Recordatorio: qué es Chebotko y por qué importa

Cassandra **no** se modela como SQL. Se modela **query-first** (metodología Chebotko): primero defines las consultas que la app necesita, y para **cada patrón de consulta diseñas una tabla**, desnormalizando lo que haga falta. Las columnas marcadas **K** son la **Partition Key** (decide en qué nodo vive el dato y cómo se distribuye la carga) y las **C** son la **Clustering Key** (ordena físicamente los datos dentro de la partición). No hay JOINs: si dos consultas necesitan el mismo dato ordenado distinto, son dos tablas.

El informe del equipo define 24 tablas para 24 queries (Q1–Q24), en un flujo de 6 etapas. Acá están **implementadas, repartidas por servicio, y con tres correcciones**.

---

## ⚠️ Correcciones aplicadas

### Fix 1 — Rankings con métrica mutable como clustering key (Q7, Q22, Q23)

**Diseño original:** las tres tablas de ranking ponen la métrica (`total_torneos`, `total_victorias`) como **clustering key** para tener el orden en disco.

**Problema:** en Cassandra **las columnas de la primary key son inmutables** — no se puede hacer `UPDATE` sobre una clustering column. Como esas métricas cambian todo el tiempo (cada inscripción, cada victoria), "actualizarlas" obligaría a **borrar la fila vieja e insertar una nueva**, y para borrarla hay que conocer el valor anterior. Frágil, sobre todo cuando la actualización llega por un evento asíncrono.

**Fix:** sacar la métrica de la clustering key y hacerla una columna **`counter`**. La tabla queda con `bucket` como partition key y el id (equipo o jugador) como clustering. El ranking se incrementa atómicamente:
```sql
UPDATE ranking_equipos_global SET total_torneos = total_torneos + 1
WHERE bucket = 'GLOBAL' AND equipo_id = ?;
```
Para el **Top-N**: se lee la partición completa (`WHERE bucket = 'GLOBAL'`) — a lo sumo unos miles de filas — y se ordena en el servicio con `OrderByDescending(...).Take(n)`. Instantáneo a este volumen.

**Trade-off (decir esto en la defensa):** perdemos el ordenamiento físico en disco por la métrica, a cambio de actualizaciones atómicas y triviales. Para este volumen es la decisión correcta.

> **Restricción de los counters:** una tabla con una columna `counter` solo puede tener columnas `counter` además de la primary key. Por eso `nombre_equipo`/`nombre_jugador` **no** van en las tablas de ranking; estas devuelven `id` + contador, y el nombre se resuelve aparte (REST) o se cachea. Lo mismo aplica a `stats_equipo_por_torneo` (Q24), que usa counters para `victorias`/`derrotas`/`partidas_jugadas`.

### Fix 2 — Q19 enfrentamientos directos (partidas_por_rivales)

**Diseño original:** `PARTITION KEY = equipo_local_id`, `CLUSTERING = (equipo_visit_id, fecha)`.

**Problema:** solo encuentra partidas donde el equipo consultado fue **local**. Si el equipo A jugó contra B pero fue visitante (B local), ese enfrentamiento **no aparece** al consultar A vs B. Para "enfrentamientos directos entre dos equipos" querés todos, sin importar la localía.

**Fix:** hacer la tabla **bidireccional** con doble escritura. La partition key pasa a ser `equipo_id` (genérico, no "local") y la clustering `rival_id`. Al registrar una partida entre A y B se insertan **dos filas**: `(equipo_id=A, rival_id=B, ...)` y `(equipo_id=B, rival_id=A, ...)`. Así `WHERE equipo_id=A AND rival_id=B` trae todos los enfrentamientos. La columna de dato `equipo_local_id` preserva quién fue realmente local.

### Fix 3 — Q18 partidas por fecha (granularidad)

**Diseño original:** `PARTITION KEY = fecha`.

**Problema:** si `fecha` es un timestamp exacto, casi cada partida cae en su propia partición (no agrupa nada). 

**Fix:** particionar por **día** (`dia date`, no timestamp), agrupando todas las partidas de esa jornada. El instante exacto se guarda aparte como `fecha timestamp` donde haga falta orden cronológico fino (Q16, Q17, Q19).

> **Convención de fechas en todo el modelo:** `fecha` = `timestamp` (instante exacto; usado como clustering en Q16/Q17/Q19 para orden cronológico). `dia` = `date` (solo la fecha; usado como partition key en Q18). `fecha_inicio` (torneos) y `fecha_creacion` (equipos) son `timestamp` e inmutables, así que sí pueden ser clustering keys.

---

## Tablas base agregadas (mejora de ingeniería)

El modelo original es 100% tablas de consulta y no tiene una "tabla por id" donde viva el registro canónico de cada entidad. Agregamos **tablas base** (lookup por id), patrón estándar en Chebotko, que sirven como fuente de verdad y como origen para alimentar las tablas de consulta:

- `jugadores` (PK `jugador_id`) y `equipos` (PK `equipo_id`) en teams.
- `videojuegos` (PK `videojuego_id`), `organizadores` (PK `organizador_id`) y `torneos` (PK `torneo_id`) en tournaments.
- `partidas` (PK `partida_id`) en matches.
- ranking no necesita tablas base: sus tablas ya son read-models derivados.

## Mapa keyspace → tablas

| Keyspace | Tablas |
|---|---|
| `esports_teams` | `jugadores`*, `equipos`*, `jugadores_por_nickname` (Q1), `jugadores_por_pais` (Q2), `jugadores_por_equipo` (Q3), `equipos_por_fecha` (Q4), `equipos_por_tag` (Q5), `integrantes_por_equipo` (Q6) |
| `esports_tournaments` | `videojuegos`*, `organizadores`*, `torneos`*, `videojuegos_por_genero` (Q8), `torneos_por_videojuego` (Q9), `organizadores_lista` (Q10), `torneos_por_organizador` (Q11), `torneos_por_fecha` (Q12), `equipos_por_torneo` (Q13), `torneos_por_equipo` (Q14), `torneo_por_codigo` (Q15), `premios_por_torneo` (Q20), `premios_por_equipo` (Q21) |
| `esports_matches` | `partidas`*, `partidas_por_torneo` (Q16), `partidas_por_equipo` (Q17), `partidas_por_fecha` (Q18), `partidas_por_rivales` (Q19) |
| `esports_ranking` | `ranking_equipos_global` (Q7), `ranking_victorias` (Q22), `ranking_jugadores_activos` (Q23), `stats_equipo_por_torneo` (Q24) |

`*` = tabla base agregada (no estaba en el informe original).

## Mapa query → tabla → servicio

| Query | Descripción | Tabla | Servicio |
|---|---|---|---|
| Q1 | Buscar jugador por nickname exacto | `jugadores_por_nickname` | teams |
| Q2 | Jugadores registrados en un país | `jugadores_por_pais` | teams |
| Q3 | Jugadores de un equipo filtrados por país | `jugadores_por_equipo` | teams |
| Q4 | Equipos por fecha de creación | `equipos_por_fecha` | teams |
| Q5 | Buscar equipo por tag | `equipos_por_tag` | teams |
| Q6 | Integrantes completos de un equipo | `integrantes_por_equipo` | teams |
| Q7 | Ranking global de equipos por torneos (Top-N) | `ranking_equipos_global` | ranking |
| Q8 | Videojuegos disponibles por género | `videojuegos_por_genero` | tournaments |
| Q9 | Torneos de un videojuego (fecha) | `torneos_por_videojuego` | tournaments |
| Q10 | Todos los organizadores | `organizadores_lista` | tournaments |
| Q11 | Torneos de un organizador | `torneos_por_organizador` | tournaments |
| Q12 | Torneos por fecha de inicio | `torneos_por_fecha` | tournaments |
| Q13 | Equipos inscritos en un torneo | `equipos_por_torneo` | tournaments |
| Q14 | Torneos en los que participó un equipo | `torneos_por_equipo` | tournaments |
| Q15 | Buscar torneo por código | `torneo_por_codigo` | tournaments |
| Q16 | Partidas de un torneo (cronológico) | `partidas_por_torneo` | matches |
| Q17 | Historial de partidas de un equipo | `partidas_por_equipo` | matches |
| Q18 | Partidas jugadas en una fecha | `partidas_por_fecha` | matches |
| Q19 | Enfrentamientos directos entre dos equipos | `partidas_por_rivales` | matches |
| Q20 | Premios de un torneo (por monto) | `premios_por_torneo` | tournaments |
| Q21 | Premios recibidos por un equipo | `premios_por_equipo` | tournaments |
| Q22 | Ranking de equipos por victorias | `ranking_victorias` | ranking |
| Q23 | Jugadores más activos por torneos | `ranking_jugadores_activos` | ranking |
| Q24 | Estadísticas de un equipo en un torneo | `stats_equipo_por_torneo` | ranking |

---

## DDL completo (CQL)

> Cada servicio ejecuta **solo el bloque de su keyspace** al arrancar, con `IF NOT EXISTS` (idempotente). RF=1 porque es single-node de desarrollo. `bucket` sintético = `'GLOBAL'` (o `'ALL'`) salvo que se indique otra cosa.

### esports_teams
```sql
CREATE KEYSPACE IF NOT EXISTS esports_teams
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Base: jugador por id
CREATE TABLE IF NOT EXISTS esports_teams.jugadores (
    jugador_id     uuid,
    nickname       text,
    nombre         text,
    pais           text,
    equipo_id      uuid,
    fecha_registro timestamp,
    PRIMARY KEY (jugador_id)
);

-- Base: equipo por id
CREATE TABLE IF NOT EXISTS esports_teams.equipos (
    equipo_id      uuid,
    nombre         text,
    tag            text,
    pais           text,
    fecha_creacion timestamp,
    PRIMARY KEY (equipo_id)
);

-- Q1: buscar jugador por nickname exacto
CREATE TABLE IF NOT EXISTS esports_teams.jugadores_por_nickname (
    nickname   text,
    jugador_id uuid,
    nombre     text,
    pais       text,
    equipo_id  uuid,
    PRIMARY KEY (nickname)
);

-- Q2: jugadores registrados en un país
CREATE TABLE IF NOT EXISTS esports_teams.jugadores_por_pais (
    pais       text,
    jugador_id uuid,
    nickname   text,
    nombre     text,
    equipo_id  uuid,
    PRIMARY KEY ((pais), jugador_id)
) WITH CLUSTERING ORDER BY (jugador_id ASC);

-- Q3: jugadores de un equipo filtrados por país
CREATE TABLE IF NOT EXISTS esports_teams.jugadores_por_equipo (
    equipo_id  uuid,
    pais       text,
    jugador_id uuid,
    nickname   text,
    nombre     text,
    PRIMARY KEY ((equipo_id), pais, jugador_id)
) WITH CLUSTERING ORDER BY (pais ASC, jugador_id ASC);

-- Q4: equipos por fecha de creación (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_teams.equipos_por_fecha (
    bucket         text,
    fecha_creacion timestamp,
    equipo_id      uuid,
    nombre         text,
    tag            text,
    pais           text,
    PRIMARY KEY ((bucket), fecha_creacion, equipo_id)
) WITH CLUSTERING ORDER BY (fecha_creacion DESC, equipo_id ASC);

-- Q5: buscar equipo por tag
CREATE TABLE IF NOT EXISTS esports_teams.equipos_por_tag (
    tag       text,
    equipo_id uuid,
    nombre    text,
    pais      text,
    PRIMARY KEY (tag)
);

-- Q6: integrantes completos de un equipo
CREATE TABLE IF NOT EXISTS esports_teams.integrantes_por_equipo (
    equipo_id  uuid,
    jugador_id uuid,
    nickname   text,
    nombre     text,
    pais       text,
    rol        text,
    PRIMARY KEY ((equipo_id), jugador_id)
) WITH CLUSTERING ORDER BY (jugador_id ASC);
```

### esports_tournaments
```sql
CREATE KEYSPACE IF NOT EXISTS esports_tournaments
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Base: videojuego por id
CREATE TABLE IF NOT EXISTS esports_tournaments.videojuegos (
    videojuego_id uuid,
    nombre        text,
    genero        text,
    PRIMARY KEY (videojuego_id)
);

-- Base: organizador por id
CREATE TABLE IF NOT EXISTS esports_tournaments.organizadores (
    organizador_id uuid,
    nombre         text,
    PRIMARY KEY (organizador_id)
);

-- Base: torneo por id
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos (
    torneo_id          uuid,
    nombre             text,
    codigo             text,
    videojuego_id      uuid,
    nombre_videojuego  text,
    organizador_id     uuid,
    nombre_organizador text,
    fecha_inicio       timestamp,
    PRIMARY KEY (torneo_id)
);

-- Q8: videojuegos disponibles por género
CREATE TABLE IF NOT EXISTS esports_tournaments.videojuegos_por_genero (
    genero        text,
    videojuego_id uuid,
    nombre        text,
    PRIMARY KEY ((genero), videojuego_id)
) WITH CLUSTERING ORDER BY (videojuego_id ASC);

-- Q9: torneos de un videojuego (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_videojuego (
    videojuego_id      uuid,
    fecha_inicio       timestamp,
    torneo_id          uuid,
    nombre_torneo      text,
    nombre_organizador text,
    PRIMARY KEY ((videojuego_id), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);

-- Q10: lista de organizadores
CREATE TABLE IF NOT EXISTS esports_tournaments.organizadores_lista (
    bucket         text,
    organizador_id uuid,
    nombre         text,
    PRIMARY KEY ((bucket), organizador_id)
) WITH CLUSTERING ORDER BY (organizador_id ASC);

-- Q11: torneos de un organizador (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_organizador (
    organizador_id    uuid,
    fecha_inicio      timestamp,
    torneo_id         uuid,
    nombre_torneo     text,
    nombre_videojuego text,
    PRIMARY KEY ((organizador_id), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);

-- Q12: torneos por fecha de inicio (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_fecha (
    bucket            text,
    fecha_inicio      timestamp,
    torneo_id         uuid,
    nombre_torneo     text,
    nombre_videojuego text,
    PRIMARY KEY ((bucket), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);

-- Q13: equipos inscritos en un torneo
CREATE TABLE IF NOT EXISTS esports_tournaments.equipos_por_torneo (
    torneo_id         uuid,
    equipo_id         uuid,
    nombre_equipo     text,
    fecha_inscripcion timestamp,
    PRIMARY KEY ((torneo_id), equipo_id)
) WITH CLUSTERING ORDER BY (equipo_id ASC);

-- Q14: torneos en los que participó un equipo (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_equipo (
    equipo_id         uuid,
    fecha_inicio      timestamp,
    torneo_id         uuid,
    nombre_torneo     text,
    nombre_videojuego text,
    PRIMARY KEY ((equipo_id), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);

-- Q15: buscar torneo por código único
CREATE TABLE IF NOT EXISTS esports_tournaments.torneo_por_codigo (
    codigo       text,
    torneo_id    uuid,
    nombre       text,
    fecha_inicio timestamp,
    PRIMARY KEY (codigo)
);

-- Q20: premios de un torneo (mayor a menor monto)
CREATE TABLE IF NOT EXISTS esports_tournaments.premios_por_torneo (
    torneo_id     uuid,
    monto         decimal,
    premio_id     uuid,
    tipo          text,
    equipo_id     uuid,
    nombre_equipo text,
    PRIMARY KEY ((torneo_id), monto, premio_id)
) WITH CLUSTERING ORDER BY (monto DESC, premio_id ASC);

-- Q21: premios recibidos por un equipo (mayor a menor monto)
CREATE TABLE IF NOT EXISTS esports_tournaments.premios_por_equipo (
    equipo_id     uuid,
    monto         decimal,
    premio_id     uuid,
    torneo_id     uuid,
    nombre_torneo text,
    tipo          text,
    PRIMARY KEY ((equipo_id), monto, premio_id)
) WITH CLUSTERING ORDER BY (monto DESC, premio_id ASC);
```

### esports_matches
```sql
CREATE KEYSPACE IF NOT EXISTS esports_matches
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Base: partida por id
CREATE TABLE IF NOT EXISTS esports_matches.partidas (
    partida_id          uuid,
    torneo_id           uuid,
    nombre_torneo       text,
    fecha               timestamp,
    dia                 date,
    equipo_local_id     uuid,
    equipo_visitante_id uuid,
    nombre_local        text,
    nombre_visitante    text,
    equipo_ganador_id   uuid,
    resultado           text,
    PRIMARY KEY (partida_id)
);

-- Q16: partidas de un torneo (cronológico, más reciente primero)
CREATE TABLE IF NOT EXISTS esports_matches.partidas_por_torneo (
    torneo_id        uuid,
    fecha            timestamp,
    partida_id       uuid,
    nombre_local     text,
    nombre_visitante text,
    resultado        text,
    PRIMARY KEY ((torneo_id), fecha, partida_id)
) WITH CLUSTERING ORDER BY (fecha DESC, partida_id ASC);

-- Q17: historial de partidas de un equipo (2 filas por partida: local y visitante)
CREATE TABLE IF NOT EXISTS esports_matches.partidas_por_equipo (
    equipo_id     uuid,
    fecha         timestamp,
    partida_id    uuid,
    nombre_torneo text,
    rival         text,
    resultado     text,
    PRIMARY KEY ((equipo_id), fecha, partida_id)
) WITH CLUSTERING ORDER BY (fecha DESC, partida_id ASC);

-- Q18 (FIX 3): partidas jugadas en un día
CREATE TABLE IF NOT EXISTS esports_matches.partidas_por_fecha (
    dia              date,
    partida_id       uuid,
    torneo_id        uuid,
    nombre_local     text,
    nombre_visitante text,
    resultado        text,
    PRIMARY KEY ((dia), partida_id)
) WITH CLUSTERING ORDER BY (partida_id ASC);

-- Q19 (FIX 2): enfrentamientos directos, bidireccional (2 filas por partida)
CREATE TABLE IF NOT EXISTS esports_matches.partidas_por_rivales (
    equipo_id       uuid,
    rival_id        uuid,
    fecha           timestamp,
    partida_id      uuid,
    equipo_local_id uuid,
    resultado       text,
    PRIMARY KEY ((equipo_id), rival_id, fecha, partida_id)
) WITH CLUSTERING ORDER BY (rival_id ASC, fecha DESC, partida_id ASC);
```

### esports_ranking
```sql
CREATE KEYSPACE IF NOT EXISTS esports_ranking
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Q7 (FIX 1): ranking global de equipos por torneos
CREATE TABLE IF NOT EXISTS esports_ranking.ranking_equipos_global (
    bucket        text,
    equipo_id     uuid,
    total_torneos counter,
    PRIMARY KEY ((bucket), equipo_id)
);

-- Q22 (FIX 1): ranking de equipos por victorias
CREATE TABLE IF NOT EXISTS esports_ranking.ranking_victorias (
    bucket          text,
    equipo_id       uuid,
    total_victorias counter,
    PRIMARY KEY ((bucket), equipo_id)
);

-- Q23 (FIX 1): jugadores más activos por torneos disputados
CREATE TABLE IF NOT EXISTS esports_ranking.ranking_jugadores_activos (
    bucket        text,
    jugador_id    uuid,
    total_torneos counter,
    PRIMARY KEY ((bucket), jugador_id)
);

-- Q24: estadísticas de un equipo por torneo (counters)
CREATE TABLE IF NOT EXISTS esports_ranking.stats_equipo_por_torneo (
    equipo_id        uuid,
    torneo_id        uuid,
    victorias        counter,
    derrotas         counter,
    partidas_jugadas counter,
    PRIMARY KEY ((equipo_id), torneo_id)
) WITH CLUSTERING ORDER BY (torneo_id ASC);
```

> Para el Top-N en Q7/Q22/Q23: leer la partición `bucket='GLOBAL'`, traer todas las filas y ordenar/recortar en el servicio. El nombre del equipo/jugador (que no está en estas tablas por la restricción de counters) se resuelve con un REST a teams si el frontend lo necesita, o se omite y el frontend lo busca por id.

## Limitaciones conocidas (mencionar en el informe, NO ocultar)

- **Particiones "GLOBAL" (Q4, Q7, Q10, Q12, Q22, Q23)**: usan un `bucket` sintético, así que todo cae en una sola partición. A escala académica (cientos/miles de filas) es perfecto. A escala real se agregaría un bucket por año (`bucket = '2026'`) para no crear una partición gigante.
- **`partidas_por_fecha` (Q18)**: una jornada con muchísimas partidas haría una partición grande; a este volumen es aceptable.
- **RF=1, un solo nodo**: no hay tolerancia a fallos real. Es entorno de desarrollo. NO afirmar "listo para producción".
- **BATCH multi-partición**: los dual-writes usan logged batch sobre tablas con distinta partition key. A gran escala Cassandra lo desaconseja por costo; a este volumen garantiza consistencia entre las copias desnormalizadas.
- **Consistencia eventual en rankings/stats**: tras una inscripción o una partida, los rankings (Q7, Q22, Q23) y las stats (Q24) se actualizan vía evento, con un desfase de milisegundos. Es el comportamiento esperado de un sistema event-driven.
- **Duplicados en counters**: RabbitMQ entrega *at-least-once*; si un evento se reentrega, un counter podría inflarse. Para la demo es aceptable; ver `docs/05` para la mitigación opcional.
