# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

---

# Esports Platform — Sistemas Distribuidos, UNIVALLE

> Este archivo es la **constitución del proyecto**. Codex lo lee automáticamente al iniciar. Cualquier agente que trabaje en este repo DEBE respetar lo que está acá. Si algo contradice estas reglas, gana este archivo.

## 1. Qué estamos construyendo

Backend de una **plataforma de gestión de torneos de eSports multiplataforma**, en arquitectura de **microservicios .NET 10** sobre **Apache Cassandra** (modelado con metodología Chebotko, query-first). El modelo cubre **24 consultas (Q1–Q24)** repartidas en **24 tablas desnormalizadas**. El backend expone una API REST a través de un **API Gateway** para que el equipo de frontend trabaje contra una sola URL. Todo corre en **Docker** y debe funcionar idéntico en **macOS (Apple Silicon)** y **Windows**.

Son **4 microservicios + 1 gateway**:

| Servicio | Keyspace | Puerto interno | Puerto host (dev) | Dominio | Queries |
|---|---|---|---|---|---|
| `teams` | `esports_teams` | 8080 | 5001 | Jugadores y equipos | Q1–Q6 |
| `tournaments` | `esports_tournaments` | 8080 | 5002 | Videojuegos, organizadores, torneos, inscripciones, premios | Q8–Q15, Q20, Q21 |
| `matches` | `esports_matches` | 8080 | 5003 | Partidas y enfrentamientos | Q16–Q19 |
| `ranking` | `esports_ranking` | 8080 | 5004 | Rankings y estadísticas (event-driven) | Q7, Q22, Q23, Q24 |
| `gateway` | — | 8080 | **8080** | Puerta de entrada única (YARP) | — |

El **frontend pega solo a `http://localhost:8080`**.

> Nota de diseño: `tournaments` es el servicio más grande (aloja también los catálogos de videojuegos y organizadores, porque solo existen en función de los torneos, y los premios, porque pertenecen al torneo). Es deliberado y defendible. Por eso en el plan de ejecución se construye después de `teams` y se le dedica más tiempo.

## 2. Stack fijo (NO cambiar versiones sin avisar)

- **.NET 10** (LTS). Imágenes: `mcr.microsoft.com/dotnet/sdk:10.0` y `mcr.microsoft.com/dotnet/aspnet:10.0`.
- **Apache Cassandra** → imagen `cassandra:5.0` (multi-arch: corre nativo en ARM y x86).
- **RabbitMQ** → imagen `rabbitmq:3-management`.
- **Driver Cassandra**: `CassandraCSharpDriver` (oficial de DataStax). **NO usar Entity Framework** — esto es Cassandra, no SQL relacional.
- **Mensajería**: `MassTransit.RabbitMQ` **versión 8.x** (Apache 2.0, gratis). ⚠️ **PROHIBIDO instalar MassTransit 9.x** — la v9 es comercial y requiere licencia de pago; rompería el build. Pinear `Version="8.*"`.
- **Gateway**: `Yarp.ReverseProxy` (reverse proxy de Microsoft, configuración por `appsettings.json`).
- **Docs de API**: Swagger/OpenAPI en cada servicio (`Swashbuckle.AspNetCore`).
- **Resiliencia**: `Polly` para retries de conexión a Cassandra al arrancar.

## 3. Cómo debe trabajar un agente acá

1. **Antes de escribir código, leé `docs/` en este orden:** `01-arquitectura` → `02-modelo-datos` → `03-convenciones` → `04-contratos-api` → `05-eventos` → `06-docker-setup`. El plan paso a paso está en `docs/07-plan-ejecucion.md`. Las convenciones de commits están en `docs/08-commits.md`.
2. **No inventes nombres.** Namespaces, tablas, columnas, rutas y eventos ya están definidos en `docs/`. Usalos tal cual.
3. **Idempotencia siempre.** Keyspaces y tablas se crean al arrancar con `CREATE ... IF NOT EXISTS`. El proyecto tiene que levantar con `docker compose up` sin pasos manuales.
4. **Un servicio se construye completo antes de pasar al siguiente.** El primer servicio (`teams`) es la **plantilla**: una vez que funciona, los demás copian su estructura.
5. **No sobre-ingenierizar.** Tenemos deadline. Un proyecto por servicio con carpetas (no Clean Architecture de varios proyectos). Lo justo para cumplir las 24 queries + alimentar el frontend.

## 4. Reglas de oro (restricciones duras)

- 🔒 **Una base por servicio.** Ningún servicio lee ni escribe el keyspace de otro. Si necesita datos ajenos: REST (lectura) o evento (asíncrono). Nunca cross-keyspace queries.
- 🔒 **Toda mutación que toque varias tablas desnormalizadas dentro del mismo servicio va en un `BATCH` de CQL.** Ej: crear un torneo escribe `torneos` + `torneos_por_videojuego` + `torneos_por_organizador` + `torneos_por_fecha` + `torneo_por_codigo` en un solo `BATCH`.
- 🔒 **Cassandra es inmutable en la primary key.** No se hace `UPDATE` sobre partition/clustering keys. (Por eso se arreglaron Q7, Q22, Q23 con counters — ver `docs/02`.)
- 🔒 **Las tablas de ranking/stats son read-models derivados.** Solo el servicio `ranking` las escribe, y solo consumiendo eventos. No tienen escritura pública.
- 🔒 **Comunicación entre servicios:** lecturas síncronas vía `HttpClient` tipado (registrado con `AddHttpClient`), nunca `new HttpClient()`. Eventos vía MassTransit.
- 🔒 **Namespaces:** `Esports.<Servicio>.Api` (PascalCase). Contratos compartidos en `Esports.Shared`.
- 🔒 **Casing:** C# = PascalCase / `_camelCase` privados. Cassandra (keyspace/tabla/columna) = `snake_case`. Docker/carpetas/rutas = `kebab-case`/minúsculas. Ver `docs/03`.
- 🔒 **Errores:** respuestas de error con `ProblemDetails` (RFC 7807). Nada de `throw` crudo hacia el cliente.
- 🔒 **Config por variables de entorno** (las inyecta `docker-compose.yml`): `Cassandra__ContactPoints`, `Cassandra__Keyspace`, `RabbitMq__Host`, `Services__<Nombre>`.
- 🔒 **Ruteo por primer segmento.** Cada prefijo `/api/<recurso>` mapea a UN solo servicio (ver `docs/04`). Una query que cruza dominios se expone bajo el prefijo del servicio **dueño de la tabla**, no anidada bajo otro recurso.
- 🔒 **Line endings = LF.** El repo tiene `.gitattributes` que fuerza LF. No commitear CRLF (rompe scripts en contenedores Linux desde Windows).
- 🔒 **Tablas counter de ranking: solo `UPDATE`, nunca `INSERT`.** La sintaxis es `UPDATE tabla SET col = col + 1 WHERE ...`. Cassandra crea la fila sola en el primer incremento. Mezclar INSERT con columnas counter es un error de CQL.
- 🔒 **SchemaInitializer corre al iniciar** con retry vía Polly (Cassandra puede tardar en estar lista). Usa `CREATE KEYSPACE IF NOT EXISTS` y `CREATE TABLE IF NOT EXISTS` — idempotente, puede correr en cada arranque.
- 🔒 **Commits en inglés, formato Conventional Commits.** Ver `docs/08-commits.md`. Ejemplos: `feat(teams): add players by country endpoint (Q3)`, `fix(ranking): increment counter via UPDATE`. El dominio del negocio (entidades, tablas, rutas) sigue en español.

## 5. Estructura del repo

```
esports-platform/
├── AGENTS.md                  # este archivo
├── USER-STORIES.md            # historias de usuario (entendimiento del producto)
├── .gitattributes             # fuerza LF (cross-platform)
├── .gitignore
├── docker-compose.yml         # cassandra + rabbitmq + 4 servicios + gateway
├── README.md                  # cómo levantar (para los compañeros)
├── docs/                      # ← documentación que leen los agentes
│   ├── 01-arquitectura.md
│   ├── 02-modelo-datos.md
│   ├── 03-convenciones.md
│   ├── 04-contratos-api.md
│   ├── 05-eventos.md
│   ├── 06-docker-setup.md
│   └── 07-plan-ejecucion.md
├── shared/
│   └── Esports.Shared/        # contratos de eventos + DTOs comunes
├── services/
│   ├── teams/Esports.Teams.Api/
│   ├── tournaments/Esports.Tournaments.Api/
│   ├── matches/Esports.Matches.Api/
│   └── ranking/Esports.Ranking.Api/
├── gateway/Esports.Gateway/
└── tools/
    └── Esports.Seeder/        # carga datos de ejemplo (opcional pero recomendado)
```

## 6. Comandos clave

```bash
docker compose up --build              # levantar todo el stack
docker compose logs -f tournaments     # logs de un servicio
docker compose exec tournaments bash   # entrar a un contenedor
docker compose exec cassandra cqlsh    # consola CQL
docker compose down                    # bajar (conserva datos)
docker compose down -v                 # bajar y BORRAR la base (reset limpio)
```

> Todo corre en Docker. **No hay flujo de desarrollo local fuera de Docker** — el proyecto debe funcionar igual en macOS (Apple Silicon) y Windows con un solo `docker compose up --build`. Ver `docs/06-docker-setup.md`.

URLs cuando está corriendo:
- Gateway (lo que usa el frontend): `http://localhost:8080`
- Swagger por servicio: `http://localhost:5001/swagger` … `5004`
- RabbitMQ management: `http://localhost:15672` (user/pass: `guest`/`guest`)

## 7. Definition of Done (proyecto completo)

- [ ] `docker compose up --build` levanta los 4 servicios + gateway + cassandra + rabbitmq sin intervención manual.
- [ ] Las 24 queries (Q1–Q24) están implementadas y devuelven datos correctos vía el gateway.
- [ ] Los flujos de evento funcionan: inscribir un equipo incrementa su ranking de torneos (Q7) y el de sus jugadores (Q23); registrar una partida actualiza victorias (Q22) y estadísticas por torneo (Q24).
- [ ] Cada servicio tiene Swagger funcional.
- [ ] El seeder carga datos de ejemplo para que el frontend tenga con qué trabajar.
- [ ] `README.md` explica cómo levantar el proyecto en Mac y en Windows.
- [ ] Todo pusheado a GitHub (`LukeHowland`).
