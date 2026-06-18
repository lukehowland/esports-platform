# ADR-001 — RF-03 con Opción B: membresías N:N temporales + código de jugador

- Estado: aceptada
- Fecha: 2026-06-17
- Contexto: defensa postergada al viernes; el profesor evalúa RF por RF y luego Q1–Q24.

## Decisión

RF-03 ("asignar uno o más jugadores a uno o más equipos, N:N") se implementa con la **Opción B**:
una entidad asociativa **membresía** (jugador↔equipo) con validez temporal (`fecha_desde`,
`fecha_hasta`). El equipo actual es la membresía activa (`fecha_hasta = null`). Regla eSports:
**un jugador tiene a lo sumo una membresía activa** — puede pasar por varios equipos en el
tiempo (Faker: T1 → Gen.G), nunca dos a la vez.

Movimiento modelado como en eSports real: **liberar (baja) → agente libre → fichar (alta)**.
Liberar cierra la membresía activa (no la borra → historial preservado) y deja al jugador sin
equipo (`equipo_id = null`). Fichar solo aplica a agentes libres (ahí se enforza la invariante).
Admin puede hacer traspaso atómico (baja+alta en un solo BATCH).

Se agrega un **código legible e inmutable `J-001`** (llave de negocio sobre el `jugador_id` UUID).
Lo asigna el sistema al registrar; **nadie lo edita** (ni admin, ni organizador, ni capitán). Es
análogo al `codigo` de torneo (Q15) y al `tag` de equipo (Q5): identidad humana estable.

## Autorización

- Registrar jugador nuevo: **admin** o **capitán del equipo** (igual que hoy).
- Liberar: **admin** o **capitán del equipo actual** del jugador.
- Fichar (asignar agente libre): **admin** o **capitán del equipo destino**.
- Traspaso atómico (jugador con equipo activo): **solo admin**.
- `codigo`: inmutable para todos.

## Por qué (no Opción A)

La Opción A (campo `equipo_id` único + log de historial) modela N:1 y contradice el MER, que
define `PERTENECE` como N:N. La Opción B es la resolución correcta de una relación N:N con
atributos, soporta historial real y es **aditiva**: no cambia ninguna PK existente ni las 24
queries. Las 5 tablas de `esports_teams` quedan como "roster activo".

## Alcance e impacto

- Aditivo sobre `esports_teams`: las 5 tablas (`jugadores`, `jugadores_por_nickname`,
  `jugadores_por_pais`, `jugadores_por_equipo`, `integrantes_por_equipo`) quedan como "roster
  activo"; `codigo` es columna nueva; `equipo_id` pasa a nullable (agente libre).
- Nuevas tablas: `membresias_por_jugador`, `jugador_por_codigo`, `secuencias`.
- Q1–Q24 sin cambios; los 143 tests deben seguir verdes.

## Estado del resto de los RF (se harán DESPUÉS de validar este al 100%)

- RF-01 Jugadores: agregar `email` + `telefono`; alta independiente + editar/eliminar. PARCIAL.
- RF-02 Equipos: CRUD admin (PUT/DELETE backend + UI). NO CUMPLE — máxima prioridad siguiente.
- RF-04 Videojuegos: agregar `plataforma`. PARCIAL.
- RF-05 Organizadores: agregar `email`. PARCIAL.
- RF-06 Torneos: agregar `fecha_fin`. PARCIAL.
- RF-07..RF-11: cumplen.

## Validación de esta decisión

`./scripts/test-clean.sh` (143 existentes + nuevos verdes) + pasada Chrome de RF-03 y Q1–Q24.
Mientras no esté 100% verde y sin regresiones, no se avanza a los otros RF.
