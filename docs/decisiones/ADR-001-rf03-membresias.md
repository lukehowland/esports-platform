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
- Q1–Q24 sin cambios; la suite completa debe seguir verde.

## Estado posterior de los RF relacionados

RF-03 se implementó y validó primero. La rama
`feat/rf-fields-and-equipos-crud` completa después, sin modificar la decisión de membresías:

- RF-01 Jugadores: `email` + `telefono`, alta, edición y eliminación con reglas de integridad.
- RF-02 Equipos: CRUD administrativo con bloqueo `409` si existe roster activo.
- RF-04 Videojuegos: `plataforma` requerida en backend, seed y UI.
- RF-05 Organizadores: `email` requerido en backend, seed y UI.
- RF-06 Torneos: `fecha_fin`, edición y eliminación con ownership y bloqueos por dependencias.
- RF-07..RF-11: permanecen cubiertos.

## Validación de esta decisión

El 19 de junio de 2026 se ejecutó `./scripts/test-clean.sh` desde volúmenes vacíos:
`169/169` tests pasaron y el script restauró el stack demo con seeder exitoso.
