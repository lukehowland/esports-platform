# 07 — Plan de Ejecución (para Claude Code)

> **Objetivo:** backend completo (4 microservicios + gateway, Cassandra, RabbitMQ, todo en Docker) que cubra las 24 queries y quede listo para que el frontend lo consuma. **Deadline: miércoles.** Este doc es la secuencia de fases que vas a ejecutar con agentes de Claude Code. Cada fase tiene objetivo, tareas, archivos, criterio de aceptación y un prompt sugerido.

## Cómo orquestar los agentes

- **Trabajá fase por fase, en orden.** No arranques una fase hasta que la anterior cumpla su criterio de aceptación. La integración es el cuello de botella, no la cantidad de código.
- **El primer servicio (`teams`) es la plantilla de oro.** Una vez que funciona end-to-end, los demás se construyen copiando su estructura. Esto hace que las fases siguientes vuelen.
- **Cada prompt arranca recordándole al agente leer los docs:** *"Leé CLAUDE.md y docs/01..06. Respetá namespaces, tablas, rutas y eventos tal como están definidos. No inventes nombres."*
- **Verificá con `docker compose up` después de cada fase de servicio.** Si no levanta, no avances.
- **Paralelizar con tus compañeros (opcional):** recién **después** de que `teams` esté hecho y mergeado, uno toma `matches`, otro ayuda con `tournaments` (el más grande), vos cerrás `ranking` + `gateway`. Copian de algo que ya funciona.

---

## Fase 0 — Scaffolding e infraestructura

**Objetivo:** estructura del repo + Cassandra y RabbitMQ levantando.

**Tareas:**
1. Crear la estructura de carpetas de `CLAUDE.md §5`.
2. `.gitattributes` (LF, docs/06), `.gitignore` (.NET), `README.md` base.
3. `docker-compose.yml` con **solo** `cassandra` y `rabbitmq` (los servicios se agregan a medida que existen).
4. `shared/Esports.Shared` (class library .NET 10) con los dos records de evento en `Esports.Shared.Events`: `TeamRegisteredToTournament` y `MatchPlayed` (docs/05).
5. Solución `Esports.sln` + agregar `Esports.Shared`.

**Criterio de aceptación:** `docker compose up` deja Cassandra healthy y RabbitMQ accesible (`:15672`); `Esports.Shared` compila.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Creá la estructura de carpetas del repo (CLAUDE.md §5), el `.gitattributes` con LF y `.gitignore` de .NET, un `docker-compose.yml` que por ahora solo levante `cassandra:5.0` y `rabbitmq:3-management` con sus healthchecks (docs/06), y la class library `Esports.Shared` (.NET 10) con los records `TeamRegisteredToTournament` y `MatchPlayed` (docs/05). Creá `Esports.sln`. Verificá que `docker compose up` deje Cassandra healthy y RabbitMQ accesible.

---

## Fase 1 — Servicio plantilla: teams (Q1–Q6, end-to-end)

**Objetivo:** `teams` 100% funcional. Es el molde que copian los demás.

**Tareas:**
1. `Esports.Teams.Api` (ASP.NET Core Web API, .NET 10) con la estructura de docs/03.
2. Paquetes: `CassandraCSharpDriver`, `Swashbuckle.AspNetCore`, `Polly`.
3. `Cassandra/SchemaInitializer.cs`: conectar con retry (Polly), crear keyspace `esports_teams` + sus 8 tablas (docs/02), idempotente, al arrancar.
4. Dominio: `Jugador`, `Equipo`. Repositories con prepared statements; alta de equipo y alta de jugador usan **`BATCH`** (docs/01).
5. Controllers + Services + DTOs para los endpoints de teams (docs/04): crear equipo, agregar jugador, Q1–Q6, get equipo by id, get integrantes.
6. Swagger + `ProblemDetails`.
7. `Dockerfile` (multi-stage, docs/06) + agregar `teams` al `docker-compose.yml` (puerto 5001). Resolver acá cómo el build incluye `Esports.Shared` y documentarlo.

**Criterio de aceptación:**
- `docker compose up --build` levanta `teams`; Swagger en `:5001/swagger`.
- Flujo: crear equipo → agregar 2-3 jugadores → Q1 (por nickname), Q2 (por país), Q3 (del equipo filtrando país), Q4 (equipos por fecha), Q5 (por tag), Q6 (integrantes) devuelven datos correctos.
- Reiniciar el contenedor no rompe nada (idempotencia).

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá el microservicio `teams` completo (estructura docs/03, contratos docs/04). CassandraCSharpDriver (NO EF), bootstrap idempotente del keyspace `esports_teams` y sus 8 tablas (docs/02) con retry vía Polly, BATCH para alta de equipo y de jugador, Swagger y ProblemDetails. Dockerfile multi-stage + servicio en compose (5001), resolviendo cómo el build referencia Esports.Shared. Verificá Q1–Q6 vía Swagger.

---

## Fase 2 — tournaments (el grande: Q8–Q15, Q20, Q21 + evento)

**Objetivo:** catálogos (videojuegos, organizadores), torneos, inscripciones, premios, y publicar `TeamRegisteredToTournament`.

**Tareas:**
1. Copiar estructura de `teams`. Keyspace `esports_tournaments` + sus 13 tablas. Organizar por sub-dominio (Videojuegos, Organizadores, Torneos, Inscripciones, Premios).
2. Paquete extra: `MassTransit.RabbitMQ` **8.*** (NO 9.x).
3. Escrituras con **`BATCH`**: crear videojuego, crear organizador, crear torneo (5 tablas), asignar premio (2 tablas).
4. Inscribir equipo → `HttpClient` tipado a teams (`GET /api/equipos/{id}` para nombre, `GET .../integrantes` para roster), **`BATCH`** `equipos_por_torneo` + `torneos_por_equipo`, y **publicar** `TeamRegisteredToTournament` con los `jugadorIds` (docs/05).
5. Lecturas: Q8, Q9, Q10, Q11, Q12, Q13, Q14, Q15, Q20, Q21 (docs/04).
6. `AddHttpClient` con `Services__Teams` + MassTransit publisher. Dockerfile + servicio en compose (5002).

**Criterio de aceptación:**
- Levanta `teams` + `tournaments`.
- Crear videojuego → Q8; crear organizador → Q10; crear torneo → Q9, Q11, Q12, Q15; asignar premio → Q20, Q21.
- Inscribir un equipo existente → 201, aparece en Q13 y Q14, y en RabbitMQ se ve el evento publicado (con jugadorIds).

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá `tournaments` copiando la estructura de `teams`. Keyspace `esports_tournaments` + 13 tablas (docs/02), organizado por sub-dominio. MassTransit.RabbitMQ 8.* (NUNCA 9.x). Crear torneo, asignar premio e inscribir usan BATCH. La inscripción pide nombre y roster del equipo a teams por HttpClient tipado y publica TeamRegisteredToTournament con los jugadorIds (docs/05). Implementá Q8–Q15, Q20, Q21 (docs/04). Dockerfile + compose (5002). Verificá las queries y que la inscripción publica el evento.

---

## Fase 3 — matches (Q16–Q19 + evento)

**Objetivo:** partidas, sus cuatro vistas, y publicar `MatchPlayed`.

**Tareas:**
1. Copiar estructura. Keyspace `esports_matches` + 5 tablas (incluida la base).
2. Paquete: `MassTransit.RabbitMQ` 8.*.
3. Registrar partida → **`BATCH`** `partidas` + `partidas_por_torneo` + `partidas_por_equipo` (**2 filas**: local y visitante, ajustando `rival`/`resultado`) + `partidas_por_fecha` (derivar `dia` del timestamp) + `partidas_por_rivales` (**2 filas** bidireccionales, FIX 2). Luego **publicar** `MatchPlayed` (docs/05).
4. (Opcional) validar torneo/equipos vía REST.
5. Lecturas: Q16, Q17, Q18 (por día `YYYY-MM-DD`), Q19 (enfrentamientos directos). Dockerfile + compose (5003).

**Criterio de aceptación:**
- Registrar una partida entre A y B → aparece en Q16 (torneo), Q17 para **ambos** equipos, Q18 (por su día), y Q19 funciona consultando **tanto A vs B como B vs A**. En RabbitMQ se ve `MatchPlayed`.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá `matches` copiando la estructura previa. Keyspace `esports_matches` + 5 tablas (docs/02). Registrar partida usa BATCH e inserta 2 filas en `partidas_por_equipo` y 2 en `partidas_por_rivales` (FIX 2, bidireccional); deriva `dia` (date) del `fecha` (timestamp) para `partidas_por_fecha` (FIX 3). Publica MatchPlayed (docs/05). MassTransit 8.*. Implementá Q16–Q19 (docs/04). Dockerfile + compose (5003). Verificá que Q19 trae enfrentamientos en ambos sentidos y que se publica el evento.

---

## Fase 4 — ranking (consumidor de eventos: Q7, Q22, Q23, Q24)

**Objetivo:** servicio event-driven que mantiene rankings y stats.

**Tareas:**
1. `Esports.Ranking.Api`. Keyspace `esports_ranking` + 4 tablas con **counters** (Q7, Q22, Q23, Q24 — FIX 1, docs/02).
2. Paquetes: `MassTransit.RabbitMQ` 8.*, `CassandraCSharpDriver`.
3. `Consumers/TeamRegisteredConsumer`: `+1` torneos del equipo (Q7) y de cada jugador del roster (Q23).
4. `Consumers/MatchPlayedConsumer`: `+1` victorias del ganador (Q22); stats de ambos equipos en el torneo (Q24).
5. Lecturas: Q7, Q22, Q23 (Top-N: leer partición GLOBAL, ordenar en memoria), Q24 (docs/04).
6. Dockerfile + servicio en compose (5004) con `RabbitMq__Host`.

**Criterio de aceptación:**
- Inscribir un equipo (Fase 2) → Q7 y Q23 reflejan el incremento.
- Registrar una partida con ganador (Fase 3) → Q22 y Q24 reflejan el resultado.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/01..06. Implementá `ranking`: keyspace `esports_ranking` con las 4 tablas usando counters (FIX 1, docs/02). MassTransit 8.* con dos consumers: TeamRegisteredConsumer (incrementa Q7 y Q23 por cada jugador) y MatchPlayedConsumer (incrementa Q22 y actualiza Q24). Implementá las lecturas Q7/Q22/Q23 (Top-N ordenando en memoria) y Q24 (docs/04). Dockerfile + compose (5004). Verificá los dos flujos de evento.

---

## Fase 5 — API Gateway (YARP)

**Objetivo:** una sola URL pública (`:8080`) que rutea a los 4 servicios.

**Tareas:**
1. `Esports.Gateway` (ASP.NET Core), paquete `Yarp.ReverseProxy`.
2. Rutas y clusters en `appsettings.json` según la tabla de **ruteo por primer segmento** de docs/04 (10 prefijos → 4 servicios). Sin solapamientos porque cada `/api/<recurso>` mapea a un solo servicio.
3. Dockerfile + servicio `gateway` en compose (8080:8080, `depends_on` los 4).

**Criterio de aceptación:** las 24 queries (Q1–Q24) funcionan pegando **solo** a `http://localhost:8080`.

**Prompt sugerido:**
> Leé CLAUDE.md y docs/04. Implementá `gateway` con Yarp.ReverseProxy, configurando rutas y clusters por appsettings.json según la tabla de ruteo por primer segmento de docs/04 (/api/jugadores y /api/equipos→teams; /api/videojuegos, /api/organizadores, /api/torneos, /api/inscripciones, /api/premios→tournaments; /api/partidas→matches; /api/ranking y /api/stats→ranking). Dockerfile + compose (8080:8080). Verificá que Q1–Q24 responden vía http://localhost:8080.

---

## Fase 6 — Seed data + README + Swagger pulido

**Objetivo:** datos de ejemplo para el frontend y documentación de arranque.

**Tareas:**
1. `tools/Esports.Seeder` (consola o script) que, vía el gateway, cargue datos realistas y conectados: videojuegos, organizadores, equipos con jugadores de distintos países, torneos, inscripciones cruzadas, premios y partidas con ganadores. Idempotente y "reset-friendly".
2. Correrlo automaticamente dentro de `docker compose up --build` para que no existan pasos manuales antes de usar el frontend.
3. `README.md`: requisitos, cómo levantar en Mac y Windows, URLs, cómo correr el seeder, link a docs/04.

**Criterio de aceptación:** tras `docker compose up --build`, las 24 queries devuelven datos no vacíos vía el gateway; un compañero en Windows levanta el proyecto siguiendo el README.

**Prompt sugerido:**
> Leé CLAUDE.md y docs. Creá `tools/Esports.Seeder` que cargue datos realistas vía el gateway (videojuegos de varios géneros, organizadores, equipos con jugadores de varios países, torneos, inscripciones, premios, partidas con ganadores) cubriendo las 24 queries. Integralo al flujo normal de `docker compose up --build`. Escribí el README con pasos para Mac y Windows, URLs y endpoints. Verificá que Q1–Q24 traen datos.

---

## Fase 7 — Smoke test end-to-end + push final

**Objetivo:** todo verde y en GitHub.

**Tareas:**
1. `docker compose down` + `docker compose up --build` desde cero → arranque limpio, repoblado y sin pasos manuales.
2. Recorrer la checklist de Definition of Done (`CLAUDE.md §7`).
3. Probar las 24 queries vía gateway + los dos flujos de evento.
4. Commit + push a GitHub (`LukeHowland`).

**Prompt sugerido:**
> Leé CLAUDE.md §7. Reset limpio (`docker compose down` y `docker compose up --build`), recorré la Definition of Done, probá las 24 queries vía el gateway y los dos flujos de evento, arreglá lo que falle, y dejá todo commiteado para push.

---

## Handoff al equipo de frontend

Cuando termine la Fase 6, pasales:
- **URL base:** `http://localhost:8080` (todo cuelga de acá).
- **Swagger por servicio:** `:5001`–`:5004/swagger`.
- **`docs/04-contratos-api.md`** (los 24 endpoints con shapes).
- **Arranque:** `docker compose up --build` deja el stack levantado y poblado.
- Recordá que rankings y stats son eventualmente consistentes (pueden tardar un instante tras una inscripción o partida).

## Orden de prioridad si el tiempo aprieta

1. **teams + tournaments + gateway** → Q1–Q6, Q8–Q15, Q20, Q21 (la mayoría de las 24 queries, el gateway y el REST entre servicios). ← núcleo mínimo presentable.
2. **matches** → Q16–Q19.
3. **ranking + eventos** → Q7, Q22, Q23, Q24. ← demuestra event-driven; vale mucho, pero es lo más recortable si algo se cae (las 4 tablas dependen de los eventos).
4. **seeder** → mejora la demo; en el peor caso se carga a mano por Swagger.
