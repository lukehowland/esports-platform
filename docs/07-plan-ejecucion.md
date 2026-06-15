# 07 — Plan de Ejecución (para Claude Code)

> **Objetivo:** backend completo (4 microservicios + gateway, Cassandra, RabbitMQ, todo en Docker) listo para que el frontend lo consuma. **Deadline: miércoles.** Este doc es la secuencia de fases que vas a ejecutar con agentes de Claude Code. Cada fase tiene objetivo, tareas, archivos, criterio de aceptación y un prompt sugerido para pegarle al agente.

## Cómo orquestar los agentes

- **Trabajá fase por fase, en orden.** No arranques la Fase 2 hasta que la Fase 1 cumpla su criterio de aceptación. La integración es el cuello de botella, no la cantidad de código.
- **El primer servicio (`teams`) es la plantilla de oro.** Una vez que funciona end-to-end, los demás se construyen copiando su estructura. Esto hace que las fases 3 y 4 sean rapidísimas.
- **Cada prompt debe empezar recordándole al agente que lea los docs.** Ejemplo de encabezado para todos los prompts: *"Leé CLAUDE.md y docs/01..06. Respetá namespaces, tablas, rutas y eventos tal como están definidos. No inventes nombres."*
- **Verificá con `docker compose up` después de cada fase de servicio.** Si no levanta, no avances.
- Si vas a paralelizar entre tus compañeros: que uno tome `tournaments`, otro `matches`, vos `teams`+`ranking`+`gateway`. Pero recién **después** de que `teams` (la plantilla) esté hecho y mergeado, para que copien de algo que ya funciona.

---

## Fase 0 — Scaffolding e infraestructura

**Objetivo:** estructura del repo + Cassandra y RabbitMQ levantando, sin servicios todavía.

**Tareas:**
1. Crear la estructura de carpetas de `CLAUDE.md` §5.
2. Crear `.gitattributes` (LF, ver `docs/06`), `.gitignore` (.NET), `README.md` base.
3. Crear `docker-compose.yml` con **solo** `cassandra` y `rabbitmq` por ahora (los servicios se agregan a medida que existen).
4. Crear el proyecto `shared/Esports.Shared` (class library .NET 10) con el record `TeamRegisteredToTournament` en `Esports.Shared.Events` (ver `docs/05`).
5. Crear la solución `Esports.sln` y agregar `Esports.Shared`.

**Criterio de aceptación:**
- `docker compose up` levanta Cassandra (healthy) y RabbitMQ (UI accesible en `:15672`).
- `dotnet build` de `Esports.Shared` compila.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Creá la estructura de carpetas del repo (CLAUDE.md §5), el `.gitattributes` con LF y `.gitignore` de .NET, un `docker-compose.yml` que por ahora solo levante `cassandra:5.0` y `rabbitmq:3-management` con sus healthchecks (docs/06), y la class library `Esports.Shared` (.NET 10) con el record de evento `TeamRegisteredToTournament` (docs/05). Creá también `Esports.sln`. Verificá que `docker compose up` deje Cassandra healthy y RabbitMQ accesible.

---

## Fase 1 — Servicio plantilla: Teams (end-to-end)

**Objetivo:** `teams` 100% funcional. Es el molde que copian los demás.

**Tareas:**
1. Crear `Esports.Teams.Api` (ASP.NET Core Web API, .NET 10) con la estructura de carpetas de `docs/03`.
2. Agregar paquetes: `CassandraCSharpDriver`, `Swashbuckle.AspNetCore`, `Polly`.
3. `Cassandra/CassandraSession.cs` + `Cassandra/SchemaInitializer.cs`: conectar con retry (Polly), crear keyspace `esports_teams` y sus 4 tablas (`docs/02`), idempotente, al arrancar.
4. Dominio: `Equipo`, `Jugador`.
5. Repositories: `EquipoRepository`, `JugadorRepository` con prepared statements. El alta de jugador usa **`BATCH`** sobre `jugadores` + `jugadores_por_equipo` + `jugadores_por_pais`.
6. Services + Controllers + DTOs para los endpoints de Teams (`docs/04`): crear equipo, get equipo by id, agregar jugador, Q3, Q10.
7. Swagger + `ProblemDetails`.
8. `Dockerfile` (multi-stage, `docs/06`) y agregar el servicio `teams` al `docker-compose.yml`.

**Criterio de aceptación:**
- `docker compose up --build` levanta `teams`; Swagger responde en `:5002/swagger`.
- Flujo: crear equipo → agregar jugador → `GET /api/equipos/{id}/jugadores?pais=Bolivia` (Q3) devuelve el jugador → `GET /api/jugadores/por-pais/Bolivia` (Q10) lo devuelve.
- Reiniciar el contenedor no rompe nada (idempotencia del schema).

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá el microservicio `teams` completo siguiendo la estructura de docs/03 y los contratos de docs/04. Usá CassandraCSharpDriver (NO EF), bootstrap idempotente del keyspace `esports_teams` y sus tablas (docs/02) con retry vía Polly al arrancar, BATCH para el alta de jugador, Swagger y ProblemDetails. Agregá su Dockerfile multi-stage y el servicio al docker-compose. Verificá el flujo crear equipo → agregar jugador → Q3 → Q10 vía Swagger.

---

## Fase 2 — Tournaments (+ publicación de evento)

**Objetivo:** torneos, premios, inscripciones, y publicar `TeamRegisteredToTournament`.

**Tareas:**
1. Copiar la estructura de `teams`. Keyspace `esports_tournaments` + sus 6 tablas.
2. Paquetes extra: `MassTransit.RabbitMQ` **8.*** (NO 9.x).
3. Crear torneo → **`BATCH`** sobre `torneos` + `torneos_por_organizador` + `torneos_por_videojuego`.
4. Agregar premio → `premios_por_torneo`.
5. Inscribir equipo → `HttpClient` tipado a Teams (`GET /api/equipos/{id}` para el nombre), **`BATCH`** sobre `equipos_por_torneo` + `torneos_por_equipo`, y **publicar** `TeamRegisteredToTournament` (docs/05).
6. Endpoints de lectura: Q1, Q2, Q5, Q6, Q7 (`docs/04`).
7. Registrar `AddHttpClient` con `Services__Teams` y MassTransit publisher. Dockerfile + servicio en compose.

**Criterio de aceptación:**
- `docker compose up --build` levanta `teams` + `tournaments`.
- Crear torneo → agregar premio → `GET .../premios` (Q6) ordenado desc.
- Inscribir un equipo existente → 201, y en RabbitMQ (`:15672`) se ve el mensaje publicado.
- Q1, Q2, Q5, Q7 devuelven datos correctos.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá `tournaments` copiando la estructura de `teams`. Keyspace `esports_tournaments` + tablas de docs/02. Usá MassTransit.RabbitMQ versión 8.* (NUNCA 9.x). Crear torneo y inscribir equipo usan BATCH (docs/01). La inscripción pide el nombre del equipo a Teams por HttpClient tipado y publica el evento TeamRegisteredToTournament (docs/05). Implementá Q1, Q2, Q5, Q6, Q7 según docs/04. Dockerfile + compose. Verificá premios ordenados (Q6) y que la inscripción publica el evento (RabbitMQ UI).

---

## Fase 3 — Matches

**Objetivo:** partidas y sus dos vistas (Q4, Q8).

**Tareas:**
1. Copiar estructura. Keyspace `esports_matches` + 3 tablas.
2. Crear partida → **`BATCH`** sobre `partidas` + `partidas_por_torneo` + **dos filas** en `partidas_por_equipo` (local y visitante; el `rival` y `resultado` se ajustan por fila).
3. (Opcional) validar torneo/equipos vía REST a Tournaments/Teams.
4. Endpoints: Q4 (`/api/torneos/{id}/partidas`), Q8 (`/api/equipos/{id}/partidas`). Dockerfile + compose.

**Criterio de aceptación:**
- Registrar una partida entre dos equipos → `GET /api/torneos/{id}/partidas` (Q4) la muestra, y `GET /api/equipos/{id}/partidas` (Q8) la muestra para **ambos** equipos.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá `matches` copiando la estructura de `teams`/`tournaments`. Keyspace `esports_matches` + tablas de docs/02. Crear partida usa BATCH e inserta DOS filas en `partidas_por_equipo` (local y visitante). Implementá Q4 y Q8 (docs/04). Dockerfile + compose. Verificá que la partida aparece para ambos equipos en Q8.

---

## Fase 4 — Ranking (consumidor de eventos)

**Objetivo:** servicio event-driven que mantiene el Top-N.

**Tareas:**
1. Crear `Esports.Ranking.Api`. Keyspace `esports_ranking` + tabla `ranking_equipos_global` (counter, **corregida**, docs/02).
2. Paquetes: `MassTransit.RabbitMQ` 8.*, `CassandraCSharpDriver`.
3. `Consumers/TeamRegisteredConsumer` que hace `UPDATE ... total_torneos + 1` (docs/05).
4. Endpoint Q9: `GET /api/ranking/global?top=n` → leer partición `bucket='GLOBAL'`, ordenar en memoria, paginar Top-N (docs/04).
5. Dockerfile + servicio en compose con `RabbitMq__Host`.

**Criterio de aceptación:**
- Inscribir un equipo (Fase 2) → el consumer procesa el evento → `GET /api/ranking/global?top=10` muestra al equipo con `totalTorneos` incrementado.
- Inscribir el mismo equipo en otro torneo lo incrementa de nuevo.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá `ranking`: keyspace `esports_ranking` con la tabla `ranking_equipos_global` usando counter (versión CORREGIDA de docs/02). Registrá MassTransit (8.*) con un consumer `TeamRegisteredConsumer` que incrementa `total_torneos` (docs/05). Implementá Q9 (`GET /api/ranking/global?top=n`) leyendo la partición GLOBAL y ordenando en memoria (docs/04). Dockerfile + compose. Verificá el flujo: inscribir equipo → ranking sube.

---

## Fase 5 — API Gateway (YARP)

**Objetivo:** una sola URL pública (`:8080`) que rutea a los 4 servicios.

**Tareas:**
1. Crear `Esports.Gateway` (ASP.NET Core), paquete `Yarp.ReverseProxy`.
2. Configurar rutas y clusters en `appsettings.json` según `docs/04` (tabla de ruteo). Cuidar el solapamiento de `/api/equipos/...` (Q2 → tournaments, Q8 → matches, resto → teams) usando matching por path con los segmentos correctos.
3. Dockerfile + servicio `gateway` en compose (mapea `8080:8080`, `depends_on` los 4 servicios).

**Criterio de aceptación:**
- Todas las queries Q1–Q10 funcionan pegando **solo** a `http://localhost:8080`.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/04. Implementá `gateway` con Yarp.ReverseProxy, configurando rutas y clusters por appsettings.json según la tabla de ruteo de docs/04 (resolviendo el solapamiento de /api/equipos: torneos→tournaments, partidas→matches, resto→teams). Dockerfile + compose (8080:8080). Verificá que Q1–Q10 responden vía http://localhost:8080.

---

## Fase 6 — Seed data + README + Swagger pulido

**Objetivo:** datos de ejemplo para el frontend y documentación de arranque.

**Tareas:**
1. `tools/Esports.Seeder` (consola o script) que, vía el gateway, cree: ~3 organizadores, ~3 videojuegos, ~6 equipos con jugadores de distintos países, ~4 torneos, inscripciones cruzadas, premios y ~10 partidas. Idempotente o "reset-friendly".
2. Forma de correrlo cross-platform: `docker compose run --rm seeder` o `dotnet run` documentado.
3. `README.md`: requisitos, cómo levantar en Mac y Windows, URLs, cómo correr el seeder, lista de endpoints (link a docs/04).

**Criterio de aceptación:**
- Tras `docker compose up` + seeder, las 10 queries devuelven datos no vacíos vía el gateway.
- Un compañero en Windows sigue el README y levanta el proyecto sin ayuda.

**Prompt sugerido:**
> Leé CLAUDE.md y docs. Creá `tools/Esports.Seeder` que cargue datos de ejemplo realistas vía el gateway (organizadores, videojuegos, equipos con jugadores de varios países, torneos, inscripciones, premios, partidas) cubriendo las 10 queries. Hacelo cross-platform (corrible con `docker compose run`). Escribí el README con pasos para Mac y Windows, URLs y endpoints. Verificá que tras seedear, Q1–Q10 traen datos.

---

## Fase 7 — Smoke test end-to-end + push final

**Objetivo:** todo verde y en GitHub.

**Tareas:**
1. `docker compose down -v` + `docker compose up --build` desde cero → confirmar arranque limpio sin pasos manuales.
2. Recorrer la checklist de Definition of Done (`CLAUDE.md §7`).
3. Probar las 10 queries vía gateway + el flujo de evento (inscripción → ranking).
4. Commit + push a GitHub (`LukeHowland`).

**Prompt sugerido:**
> Leé CLAUDE.md §7. Hacé un reset limpio (`docker compose down -v` y `up --build`), recorré la checklist de Definition of Done, probá las 10 queries vía el gateway y el flujo de evento, arreglá lo que falle, y dejá todo commiteado para push.

---

## Handoff al equipo de frontend

Cuando termine la Fase 6, pasales:
- **URL base:** `http://localhost:8080` (todo cuelga de acá).
- **Swagger por servicio:** `:5001`–`:5004/swagger` (para ver shapes exactos).
- **`docs/04-contratos-api.md`** (la lista de endpoints con request/response).
- **Instrucción de arranque:** `docker compose up --build` + correr el seeder.
- Recordarles que el ranking es eventualmente consistente (puede tardar un instante tras una inscripción).

## Orden de prioridad si el tiempo aprieta

Si llegás justo al miércoles, este es el orden de lo que NO se puede dejar de tener:
1. **Teams + Tournaments + Gateway** con Q1, Q2, Q3, Q5, Q6, Q7, Q10 (el grueso de las queries y la demo del gateway + REST entre servicios). ← núcleo mínimo presentable.
2. **Matches** (Q4, Q8).
3. **Ranking + evento** (Q9). ← es lo que demuestra event-driven; vale mucho, pero es lo más "recortable" si algo se cae.
4. **Seeder** (mejora la demo pero se puede cargar a mano por Swagger en el peor caso).
