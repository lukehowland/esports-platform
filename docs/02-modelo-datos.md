# 02 — Modelo de Datos (Cassandra / Chebotko)

## Recordatorio: qué es Chebotko y por qué importa

Cassandra **no** se modela como SQL. Se modela **query-first** (metodología Chebotko): primero defines las consultas que la app necesita, y para **cada patrón de consulta diseñas una tabla**, desnormalizando lo que haga falta. Las columnas marcadas **K** son la **Partition Key** (decide en qué nodo vive el dato y cómo se distribuye la carga) y las **C** son la **Clustering Key** (ordena físicamente los datos dentro de la partición). No hay JOINs: si dos consultas necesitan el mismo dato ordenado distinto, son dos tablas.

El equipo ya entregó un diagrama Chebotko con 10 tablas para 10 queries. Acá está **implementado, repartido por servicio, y con una corrección**.

## ⚠️ Corrección aplicada: Q9 (ranking_equipos_global)

**Diseño original:** `PARTITION KEY = bucket`, `CLUSTERING KEY = (total_torneos DESC, equipo_id)`.

**Problema:** en Cassandra, **las columnas de la primary key son inmutables** — no se puede hacer `UPDATE` sobre una clustering column. Como `total_torneos` cambia cada vez que un equipo se inscribe a un torneo, para "actualizarlo" habría que **borrar la fila vieja e insertar una nueva**, y para borrar la vieja hay que conocer su valor anterior. Eso es frágil y feo, sobre todo cuando la actualización llega por un evento asíncrono (el Ranking tendría que andar guardando el valor previo de cada equipo solo para poder borrarlo).

**Fix:** sacar `total_torneos` de la clustering key y dejarlo como **columna normal**. La tabla queda:
- `PARTITION KEY = bucket`
- `CLUSTERING KEY = equipo_id ASC`
- `total_torneos` → columna regular

Ahora el Ranking actualiza con un simple counter:
```sql
UPDATE ranking_equipos_global SET total_torneos = total_torneos + 1
WHERE bucket = 'GLOBAL' AND equipo_id = ?;
```
Para el **Top-N**: se lee la partición completa (`WHERE bucket = 'GLOBAL'`) — son a lo sumo unos miles de equipos — y se ordena en el servicio con `OrderByDescending(x => x.TotalTorneos).Take(n)`. Un ordenamiento en memoria de unos miles de filas es instantáneo.

**Trade-off (decir esto en la defensa):** perdemos el ordenamiento físico en disco por `total_torneos`, a cambio de actualizaciones triviales. Para este volumen es la decisión correcta. (Si quisieran ordenamiento en la base a gran escala, se haría una tabla-vista aparte mantenida con delete+insert, pero no lo necesitamos.)

> Nota sobre `nombre_equipo` en esta tabla: como usamos un **counter** para `total_torneos`, Cassandra exige que en una tabla con counters **todas las columnas no-PK sean counters**. Por eso `nombre_equipo` **no** va en `ranking_equipos_global`; el Ranking devuelve `equipo_id` + `total_torneos`, y si el frontend necesita el nombre lo resuelve contra Teams/Tournaments, o el Ranking lo cachea por evento. (Ver `docs/05`.) Si prefieren no usar counter, usen `total_torneos int` y un `UPDATE ... SET total_torneos = ?` con el valor calculado por el servicio; en ese caso sí pueden incluir `nombre_equipo`. **Recomendado: counter** (más simple y atómico).

## Tablas base agregadas (mejora de ingeniería)

El modelo original es 100% tablas de consulta y no tiene una "tabla por id" donde viva el registro canónico de cada entidad. Eso complica un `GET /equipos/{id}` y los dual-writes. Agregamos cuatro **tablas base** (lookup por id), que es un patrón estándar en Chebotko:

- `equipos` (PK `equipo_id`) y `jugadores` (PK `jugador_id`) en Teams.
- `torneos` (PK `torneo_id`) en Tournaments.
- `partidas` (PK `partida_id`) en Matches.

Sirven como fuente de verdad por id y como origen para alimentar las tablas de consulta.

## Mapa keyspace → tablas

| Keyspace | Tablas |
|---|---|
| `esports_teams` | `equipos`, `jugadores`, `jugadores_por_equipo` (Q3), `jugadores_por_pais` (Q10) |
| `esports_tournaments` | `torneos`, `torneos_por_organizador` (Q5), `torneos_por_videojuego` (Q7), `premios_por_torneo` (Q6), `equipos_por_torneo` (Q1), `torneos_por_equipo` (Q2) |
| `esports_matches` | `partidas`, `partidas_por_torneo` (Q4), `partidas_por_equipo` (Q8) |
| `esports_ranking` | `ranking_equipos_global` (Q9, corregida) |

## Mapa query → tabla → servicio

| Query | Descripción | Tabla | Servicio |
|---|---|---|---|
| Q1 | Equipos inscritos en un torneo | `equipos_por_torneo` | Tournaments |
| Q2 | Torneos de un equipo (fecha reciente) | `torneos_por_equipo` | Tournaments |
| Q3 | Jugadores de un equipo filtrados por país | `jugadores_por_equipo` | Teams |
| Q4 | Partidas de un torneo (cronológico) | `partidas_por_torneo` | Matches |
| Q5 | Torneos por organizador (fecha reciente) | `torneos_por_organizador` | Tournaments |
| Q6 | Premios de un torneo (mayor a menor monto) | `premios_por_torneo` | Tournaments |
| Q7 | Torneos por videojuego (fecha inicio) | `torneos_por_videojuego` | Tournaments |
| Q8 | Historial de partidas de un equipo | `partidas_por_equipo` | Matches |
| Q9 | Ranking global de equipos (Top-N) | `ranking_equipos_global` | Ranking |
| Q10 | Jugadores registrados por país | `jugadores_por_pais` | Teams |

## DDL completo (CQL)

> Cada servicio ejecuta **solo el bloque de su keyspace** al arrancar, con `IF NOT EXISTS` (idempotente). RF=1 porque es single-node de desarrollo.

### esports_teams
```sql
CREATE KEYSPACE IF NOT EXISTS esports_teams
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Tabla base: equipo por id (fuente de verdad)
CREATE TABLE IF NOT EXISTS esports_teams.equipos (
    equipo_id   uuid,
    nombre      text,
    pais        text,
    fecha_creacion timestamp,
    PRIMARY KEY (equipo_id)
);

-- Tabla base: jugador por id
CREATE TABLE IF NOT EXISTS esports_teams.jugadores (
    jugador_id  uuid,
    nombre      text,
    nickname    text,
    email       text,
    pais        text,
    equipo_id   uuid,
    PRIMARY KEY (jugador_id)
);

-- Q3: jugadores de un equipo filtrados por país
CREATE TABLE IF NOT EXISTS esports_teams.jugadores_por_equipo (
    equipo_id   uuid,
    pais        text,
    jugador_id  uuid,
    nombre      text,
    nickname    text,
    PRIMARY KEY ((equipo_id), pais, jugador_id)
) WITH CLUSTERING ORDER BY (pais ASC, jugador_id ASC);

-- Q10: jugadores registrados por país
CREATE TABLE IF NOT EXISTS esports_teams.jugadores_por_pais (
    pais          text,
    jugador_id    uuid,
    nombre        text,
    nickname      text,
    nombre_equipo text,
    PRIMARY KEY ((pais), jugador_id)
) WITH CLUSTERING ORDER BY (jugador_id ASC);
```

### esports_tournaments
```sql
CREATE KEYSPACE IF NOT EXISTS esports_tournaments
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Tabla base: torneo por id
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos (
    torneo_id        uuid,
    nombre           text,
    organizador_id   uuid,
    nombre_organizador text,
    videojuego_id    uuid,
    nombre_videojuego text,
    fecha_inicio     timestamp,
    PRIMARY KEY (torneo_id)
);

-- Q5: torneos por organizador (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_organizador (
    organizador_id    uuid,
    fecha_inicio      timestamp,
    torneo_id         uuid,
    nombre_torneo     text,
    nombre_videojuego text,
    PRIMARY KEY ((organizador_id), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);

-- Q7: torneos por videojuego (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_videojuego (
    videojuego_id      uuid,
    fecha_inicio       timestamp,
    torneo_id          uuid,
    nombre_torneo      text,
    nombre_organizador text,
    PRIMARY KEY ((videojuego_id), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);

-- Q6: premios de un torneo (mayor a menor monto)
CREATE TABLE IF NOT EXISTS esports_tournaments.premios_por_torneo (
    torneo_id  uuid,
    monto      decimal,
    premio_id  uuid,
    tipo       text,
    nombre_torneo text,
    PRIMARY KEY ((torneo_id), monto, premio_id)
) WITH CLUSTERING ORDER BY (monto DESC, premio_id ASC);

-- Q1: equipos inscritos en un torneo
CREATE TABLE IF NOT EXISTS esports_tournaments.equipos_por_torneo (
    torneo_id        uuid,
    equipo_id        uuid,
    nombre_equipo    text,
    fecha_inscripcion timestamp,
    PRIMARY KEY ((torneo_id), equipo_id)
) WITH CLUSTERING ORDER BY (equipo_id ASC);

-- Q2: torneos de un equipo (más reciente primero)
CREATE TABLE IF NOT EXISTS esports_tournaments.torneos_por_equipo (
    equipo_id         uuid,
    fecha_inicio      timestamp,
    torneo_id         uuid,
    nombre_torneo     text,
    nombre_videojuego text,
    PRIMARY KEY ((equipo_id), fecha_inicio, torneo_id)
) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC);
```

### esports_matches
```sql
CREATE KEYSPACE IF NOT EXISTS esports_matches
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Tabla base: partida por id
CREATE TABLE IF NOT EXISTS esports_matches.partidas (
    partida_id        uuid,
    torneo_id         uuid,
    nombre_torneo     text,
    fecha             timestamp,
    equipo_local_id   uuid,
    equipo_visitante_id uuid,
    nombre_equipo_local text,
    nombre_equipo_visitante text,
    resultado         text,
    PRIMARY KEY (partida_id)
);

-- Q4: partidas de un torneo (cronológico, más reciente primero)
CREATE TABLE IF NOT EXISTS esports_matches.partidas_por_torneo (
    torneo_id         uuid,
    fecha             timestamp,
    partida_id        uuid,
    equipo_local      text,
    equipo_visitante  text,
    resultado         text,
    PRIMARY KEY ((torneo_id), fecha, partida_id)
) WITH CLUSTERING ORDER BY (fecha DESC, partida_id ASC);

-- Q8: historial de partidas de un equipo
-- (al crear una partida se insertan DOS filas: una para el local y una para el visitante)
CREATE TABLE IF NOT EXISTS esports_matches.partidas_por_equipo (
    equipo_id     uuid,
    fecha         timestamp,
    partida_id    uuid,
    nombre_torneo text,
    rival         text,
    resultado     text,
    PRIMARY KEY ((equipo_id), fecha, partida_id)
) WITH CLUSTERING ORDER BY (fecha DESC, partida_id ASC);
```

### esports_ranking
```sql
CREATE KEYSPACE IF NOT EXISTS esports_ranking
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

-- Q9 (CORREGIDA): ranking global con counter
CREATE TABLE IF NOT EXISTS esports_ranking.ranking_equipos_global (
    bucket         text,
    equipo_id      uuid,
    total_torneos  counter,
    PRIMARY KEY ((bucket), equipo_id)
);
-- bucket siempre = 'GLOBAL'. total_torneos se incrementa con UPDATE ... + 1.
-- Top-N: leer toda la partición y ordenar en el servicio.
```

## Limitaciones conocidas (mencionar en el informe, NO ocultar)

- **`jugadores_por_pais`**: `pais` como única partition key puede generar particiones grandes para países muy poblados de jugadores. A escala de este proyecto es aceptable; a escala real se agregaría un bucket (ej. `pais + año_registro`).
- **RF=1, un solo nodo**: no hay tolerancia a fallos real. Es entorno de desarrollo. NO afirmar "listo para producción".
- **BATCH multi-partición**: los dual-writes usan logged batch sobre tablas con distinta partition key. A gran escala Cassandra lo desaconseja por costo; a este volumen está bien y garantiza consistencia entre las copias desnormalizadas.
- **Consistencia eventual en el ranking**: tras inscribir un equipo, el `total_torneos` se actualiza vía evento, así que puede haber un desfase de milisegundos. Es el comportamiento esperado de un sistema event-driven.
