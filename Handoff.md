# Handoff - cierre RF-01 a RF-06 y continuidad del proyecto

Fecha: 2026-06-19
Repositorio: `/Users/lukesito/dev/src/github.com/lukehowland/esports-platform`
Rama actual: `feat/rf-fields-and-equipos-crud`
Base: `377646e` (`origin/main`, PR #8 de RF-03)
Checkpoint previo: `checkpoint/pre-rf-resto`
Estado: cambios implementados y verificados, todavia sin commits en esta rama.

## Por que existe este handoff

Claude se quedo sin limite mientras ejecutaba el plan de los requerimientos funcionales restantes y
no pudo dejar un cierre. Codex reconstruyo el contexto leyendo el PDF entregado, el plan, la
documentacion, el historial Git, el backend, el frontend, el seeder y los tests. Luego completo los
huecos encontrados y verifico el proyecto desde una base Cassandra vacia.

Este archivo reemplaza el handoff anterior, que describia una rama ya integrada y una suite vieja
de 143 tests. La fuente versionada de los requerimientos entregados quedo en
`docs/00-requerimientos-entregados.md`.

## Estado ejecutivo

La rama completa los campos y flujos que faltaban para defender RF-01, RF-02, RF-04, RF-05 y
RF-06 sin romper RF-03 ni Q1-Q24:

- RF-01: jugadores con email y telefono, alta, edicion y eliminacion controlada.
- RF-02: CRUD administrativo de equipos.
- RF-03: codigo `J-001` y membresias temporales N:N ya integrados en `main`.
- RF-04: videojuegos con plataforma obligatoria.
- RF-05: organizadores con email obligatorio.
- RF-06: torneos con fecha de inicio y fin, edicion y eliminacion controlada.
- RF-07 a RF-11: permanecen cubiertos por los flujos existentes.

El build de produccion del frontend pasa, los servicios modificados publican correctamente y la
suite limpia completa pasa `169/169`.

## Verificacion realizada

### Build sin cache

Se ejecuto:

```bash
docker compose build --no-cache teams tournaments frontend seeder tests
```

Resultado:

- `teams` compilo y publico.
- `tournaments` compilo y publico.
- `seeder` compilo y publico.
- la imagen de tests se construyo.
- Next.js compilo, paso lint y type-check, y genero 23 rutas.

### Integracion desde cero

Se ejecuto:

```bash
./scripts/test-clean.sh
```

El script:

1. hizo `docker compose down -v`;
2. reconstruyo servicios e imagen de tests;
3. creo esquemas desde cero;
4. ejecuto el seeder;
5. corrio toda la suite;
6. elimino los datos producidos por tests;
7. restauro una demo limpia y poblada.

Resultado final:

```text
Test Run Successful.
Total tests: 169
Passed: 169
Failed: 0
```

En la primera pasada hubo un solo fallo heredado: un test afirmaba que no existia ningun
`DELETE /api/equipos/{id}`. RF-02 ahora implementa esa ruta y, sin token, responde correctamente
`401`. El test se actualizo para certificar el contrato nuevo y la segunda pasada quedo verde.

### Stack restaurado

Al terminar la suite quedaron levantados y saludables:

- Cassandra
- RabbitMQ
- auth
- teams
- tournaments
- matches
- ranking
- gateway
- frontend

El seeder finalizo con codigo 0.

### Verificacion visual

Se recorrio el frontend real en Docker tanto en escritorio como en un viewport movil de
390 x 844:

- catalogo publico de videojuegos con plataforma;
- organizadores publicos con email;
- detalle de torneo con rango inicio-fin;
- listado y detalle de jugadores con codigo, email y telefono;
- login demo de admin;
- CRUD de equipos;
- formulario de creacion de torneo;
- edicion de jugador con los datos completos;
- administracion de videojuegos, organizadores y torneos.

La primera pasada detecto que el sidebar fijo aplastaba el contenido en movil. Se corrigio el
layout para usar navegacion horizontal desplazable en pantallas pequenas, los formularios
colapsan a una columna y los dialogos respetan margen lateral. Se reconstruyo el frontend,
Next.js volvio a pasar compilacion/lint/type-check y la segunda pasada no mostro solapamientos ni
errores de consola.

## Dataset limpio confirmado

El ultimo seed restaurado reporto:

```text
Equipos: 41
Torneos: 13
Organizadores: 7
Ranking equipos por torneos: 41
Ranking equipos por victorias: 40
Ranking jugadores activos: 50
```

El dataset mantiene los juegos y torneos de LoL, CS2, Valorant, Dota 2 y Rocket League, incluido
el showcase T1 vs Gen.G. Los UUID cambian despues de `down -v`; siempre deben resolverse por las
APIs o por codigos/tags de negocio.

Usuarios demo:

```text
admin      / admin-dev-password
org_riot   / OrgDemo2024
cap_t1     / CapDemo2024
fan_demo   / FanDemo2024
```

Los patrones completos siguen siendo `org_<code>` y `cap_<tag>`.

## Cambios de backend

### RF-01 - jugadores

Campos nuevos y obligatorios:

- `email`
- `telefono`

Rutas relevantes:

```text
POST   /api/equipos/{equipoId}/jugadores
GET    /api/jugadores/{jugadorId}
PUT    /api/jugadores/{jugadorId}
DELETE /api/jugadores/{jugadorId}
```

Reglas:

- admin puede administrar cualquier jugador;
- un capitan solo puede editar jugadores de su equipo;
- eliminar es solo para admin;
- un jugador con membresia activa no se elimina: devuelve `409`;
- primero se libera, queda como agente libre y entonces puede eliminarse;
- editar conserva el codigo legible e inmutable y actualiza todas las tablas desnormalizadas.

El frontend ya consulta el detalle completo antes de abrir la edicion; anteriormente intentaba
editar desde un DTO de roster que no contenia email ni telefono.

### RF-02 - equipos

Rutas nuevas:

```text
PUT    /api/equipos/{equipoId}
DELETE /api/equipos/{equipoId}
```

Reglas:

- solo admin;
- editar mantiene coherentes `equipos`, `equipos_por_fecha` y `equipo_por_tag`;
- si cambia el tag, se elimina el indice viejo antes de escribir el nuevo;
- borrar un equipo con roster activo devuelve `409`;
- un equipo vacio puede editarse y eliminarse.

Limitacion deliberada: `teams` no consulta keyspaces ajenos. El bloqueo comprobado por RF-02 es
el roster activo dentro de `esports_teams`; no se introdujo una dependencia circular hacia
`tournaments` para buscar historia o inscripciones externas.

### RF-04 - videojuegos

Campo nuevo y obligatorio:

- `plataforma`

Se persiste tanto en la tabla principal como en `videojuegos_por_genero`. Crear, editar y eliminar
siguen siendo operaciones exclusivas de admin. Las entidades con torneos dependientes conservan
el bloqueo `409`.

### RF-05 - organizadores

Campo nuevo y obligatorio:

- `email`

Se persiste en la tabla principal y en la lista desnormalizada. Crear, editar y eliminar siguen
siendo operaciones exclusivas de admin. Los organizadores con torneos conservan el bloqueo `409`.

### RF-06 - torneos

Campo nuevo y obligatorio:

- `fecha_fin`

Rutas nuevas:

```text
PUT    /api/torneos/{id}
DELETE /api/torneos/{id}
```

Reglas:

- `fecha_fin` debe ser posterior a `fecha_inicio`;
- admin puede administrar cualquier torneo;
- organizador solo puede administrar torneos propios;
- editar o borrar un torneo con inscripciones, partidas o premios devuelve `409`;
- las mutaciones actualizan en BATCH las tablas desnormalizadas correspondientes;
- el codigo del torneo permanece como llave de negocio estable.

## Cambios de Cassandra y compatibilidad

Los `SchemaInitializer` siguen siendo idempotentes y ejecutan `CREATE ... IF NOT EXISTS`.
Tambien agregan columnas de forma aditiva con `ALTER TABLE ... ADD` tolerando que ya existan:

- jugadores: `email`, `telefono`;
- videojuegos: `plataforma`;
- organizadores: `email`;
- torneos: `fecha_fin`.

RF-03 mantiene:

- `jugador_por_codigo`;
- `membresias_por_jugador`;
- `secuencias`;
- roster activo en las tablas Q1-Q6;
- una sola membresia activa por jugador.

No se cambio ninguna primary key de Cassandra ni se agregaron lecturas cross-keyspace.

## Cambios de frontend

### Sitio publico

- detalle de jugador muestra email y telefono;
- organizadores muestran email;
- videojuegos muestran plataforma;
- detalle de torneo muestra rango inicio-fin.

### Admin

- `/panel/equipos`: alta, listado, edicion y eliminacion con confirmacion;
- `/panel/equipos/[id]`: edicion del equipo y administracion de roster;
- `/panel/organizadores`: formularios con email;
- `/panel/videojuegos`: formularios y tarjetas con plataforma;
- `/panel/torneos/[id]`: edicion y eliminacion con fecha fin;
- los mensajes `409` del backend se presentan al usuario.
- el workspace es responsive: navegacion horizontal en movil y sidebar en escritorio.

### Capitan

- `/mi-equipo`: alta de jugador con contacto;
- edicion de jugador carga primero el detalle real;
- liberar y fichar agentes libres respeta RF-03;
- eliminar agente libre pide confirmacion y queda reservado a admin.

### Organizador

- crear torneo exige fecha inicio y fecha fin;
- editar/eliminar se limita al torneo propio por JWT y ownership del backend;
- no puede crear videojuegos ni organizadores.

## Cobertura de tests agregada o reforzada

La suite cubre, entre otros:

- email y telefono de jugador;
- edicion de contacto;
- eliminacion solo de agente libre;
- ownership de capitan al editar jugador;
- codigo de jugador e indices RF-03;
- CRUD de equipo, cambio de tag y bloqueos `401/403/409`;
- plataforma requerida;
- email de organizador requerido;
- fecha fin y validacion temporal;
- edicion/eliminacion de torneo propio;
- bloqueo de torneo con dependencias;
- persistencia por codigo e indices desnormalizados;
- RBAC de admin, organizador, capitan y fan;
- Q1-Q24 y showcase live.

Persisten cuatro warnings de analizadores xUnit por `Assert.True` heredados. No afectan el
resultado de la suite.

## Documentacion actualizada

- `README.MD`
- `USER-STORIES.md`
- `docs/00-requerimientos-entregados.md`
- `docs/01-arquitectura.md`
- `docs/02-modelo-datos.md`
- `docs/04-contratos-api.md`
- `docs/decisiones/ADR-001-rf03-membresias.md`
- `frontend/MANUAL-USUARIO.md`
- `Handoff.md`

La matriz de `docs/00-requerimientos-entregados.md` debe usarse durante la defensa para recorrer
RF por RF y luego Q1-Q24.

## Archivos de implementacion modificados

Backend `teams`:

- schema, dominio y DTOs de jugadores/equipos;
- controladores de equipos y jugadores;
- repositorios de equipo y jugador;
- servicios de equipo y jugador;
- `MutacionResultado` y extension de `MovimientoResultado`.

Backend `tournaments`:

- schema y modelos;
- DTOs de videojuego, organizador y torneo;
- controladores de organizadores y torneos;
- repositorios y servicios de videojuegos, organizadores y torneos.

Frontend:

- clientes API de equipos y torneos;
- superficies publicas de jugador, organizador, videojuego y torneo;
- paneles de equipos, organizadores, videojuegos y torneos;
- creacion de torneo;
- `RosterManager`;
- manual de usuario.

Datos y pruebas:

- `tools/Esports.Seeder/Program.cs`;
- `AdminCrudTests.cs`;
- `AuthTests.cs`;
- `ErrorTests.cs`;
- `TeamsTests.cs`;
- `TournamentsTests.cs`.

## Lo que no se debe deshacer

- No volver al modelo de un jugador pegado para siempre a un solo equipo.
- No permitir dos membresias activas simultaneas.
- No hacer editable el codigo `J-001`.
- No borrar jugadores con equipo activo.
- No borrar equipos con roster activo.
- No borrar o editar catalogos con torneos dependientes.
- No permitir torneos con fecha fin anterior o igual al inicio.
- No mover autorizacion al gateway ni confiar solo en ocultar botones.
- No escribir keyspaces de otros servicios.
- No convertir tablas counter de ranking a `INSERT`.
- No hardcodear UUID del seeder.

## Trabajo pendiente inmediato

1. Revisar `git diff` y separar commits atomicos conforme a `docs/08-commits.md`.
2. Crear PR de `feat/rf-fields-and-equipos-crud` hacia `main`.
3. Preparar una hoja de defensa RF-01 a RF-11 y Q1-Q24 usando la matriz versionada.

Propuesta de commits atomicos:

```text
feat(teams): complete player and team management requirements
feat(tournaments): add required fields and tournament lifecycle
feat(frontend): expose RF management fields and workflows
test(gateway): cover remaining functional requirements
docs: align requirements, contracts and user guidance
```

## Comandos para retomar

```bash
git status --short --branch
docker compose ps
./scripts/test-clean.sh
docker compose logs --tail=100 seeder
```

URLs:

```text
Frontend:  http://localhost:3000
Gateway:   http://localhost:8080
Swagger:   http://localhost:5001/swagger ... http://localhost:5005/swagger
RabbitMQ:  http://localhost:15672
```

## Estado Git que debe esperarse

Todos los cambios de esta rama siguen sin commit para preservar exactamente el trabajo recuperado.
No hacer reset ni checkout destructivo. Revisar y agrupar por dominio antes de commitear.
