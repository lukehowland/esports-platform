# 06 — Docker (cross-platform: Mac + Windows)

> Objetivo: `git clone` + `docker compose up --build` levanta TODO, igual en **macOS (Apple Silicon)** y en **Windows**, sin pasos manuales.

## Requisitos

- **Docker Desktop** instalado.
  - Mac (M-series): nativo ARM, no necesita nada extra.
  - Windows: usar el **backend WSL2** (default de Docker Desktop). Habilitar el sharing del disco donde esté el repo.
- ~6 GB de RAM libres para Docker (Cassandra + RabbitMQ + 5 contenedores .NET).

## Por qué funciona igual en Mac y Windows

- Imágenes base **multi-arch**: `cassandra:5.0`, `rabbitmq:3-management`, `mcr.microsoft.com/dotnet/sdk:10.0` y `aspnet:10.0` traen variantes `linux/arm64` (Mac) y `linux/amd64` (Windows). Docker baja la correcta automáticamente. **No emulación, no Rosetta.**
- Todo corre dentro de contenedores **Linux** en ambos sistemas → comportamiento idéntico.
- El único riesgo real cross-platform son los **fin de línea (CRLF vs LF)**: lo resolvemos con `.gitattributes`.

## `.gitattributes` (CRÍTICO — crear en la raíz)

```gitattributes
* text=auto eol=lf
*.sh text eol=lf
*.cs text eol=lf
*.csproj text eol=lf
*.json text eol=lf
*.yml text eol=lf
*.yaml text eol=lf
Dockerfile text eol=lf
*.png binary
*.jpg binary
*.ico binary
```

## `docker-compose.yml` (raíz del repo)

```yaml
services:
  cassandra:
    image: cassandra:5.0
    container_name: esports-cassandra
    ports: ["9042:9042"]
    environment:
      CASSANDRA_CLUSTER_NAME: esports-cluster
      MAX_HEAP_SIZE: 512M
      HEAP_NEWSIZE: 128M
    volumes:
      - cassandra-data:/var/lib/cassandra
    healthcheck:
      test: ["CMD-SHELL", "cqlsh -e 'describe keyspaces' || exit 1"]
      interval: 15s
      timeout: 10s
      retries: 12
      start_period: 90s
    networks: [esports-net]

  rabbitmq:
    image: rabbitmq:3-management
    container_name: esports-rabbitmq
    ports: ["5672:5672", "15672:15672"]
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 15s
      timeout: 10s
      retries: 8
      start_period: 30s
    networks: [esports-net]

  teams:
    build: { context: ./services/teams/Esports.Teams.Api, target: dev }
    container_name: esports-teams
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://0.0.0.0:8080
      DOTNET_USE_POLLING_FILE_WATCHER: "1"
      Cassandra__ContactPoints: cassandra
      Cassandra__Keyspace: esports_teams
    volumes: ["./services/teams/Esports.Teams.Api:/src"]
    ports: ["5001:8080"]
    depends_on:
      cassandra: { condition: service_healthy }
    networks: [esports-net]

  tournaments:
    build: { context: ./services/tournaments/Esports.Tournaments.Api, target: dev }
    container_name: esports-tournaments
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://0.0.0.0:8080
      DOTNET_USE_POLLING_FILE_WATCHER: "1"
      Cassandra__ContactPoints: cassandra
      Cassandra__Keyspace: esports_tournaments
      RabbitMq__Host: rabbitmq
      Services__Teams: http://teams:8080
    volumes: ["./services/tournaments/Esports.Tournaments.Api:/src"]
    ports: ["5002:8080"]
    depends_on:
      cassandra: { condition: service_healthy }
      rabbitmq:  { condition: service_healthy }
    networks: [esports-net]

  matches:
    build: { context: ./services/matches/Esports.Matches.Api, target: dev }
    container_name: esports-matches
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://0.0.0.0:8080
      DOTNET_USE_POLLING_FILE_WATCHER: "1"
      Cassandra__ContactPoints: cassandra
      Cassandra__Keyspace: esports_matches
      RabbitMq__Host: rabbitmq
      Services__Teams: http://teams:8080
      Services__Tournaments: http://tournaments:8080
    volumes: ["./services/matches/Esports.Matches.Api:/src"]
    ports: ["5003:8080"]
    depends_on:
      cassandra: { condition: service_healthy }
      rabbitmq:  { condition: service_healthy }
    networks: [esports-net]

  ranking:
    build: { context: ./services/ranking/Esports.Ranking.Api, target: dev }
    container_name: esports-ranking
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://0.0.0.0:8080
      DOTNET_USE_POLLING_FILE_WATCHER: "1"
      Cassandra__ContactPoints: cassandra
      Cassandra__Keyspace: esports_ranking
      RabbitMq__Host: rabbitmq
    volumes: ["./services/ranking/Esports.Ranking.Api:/src"]
    ports: ["5004:8080"]
    depends_on:
      cassandra: { condition: service_healthy }
      rabbitmq:  { condition: service_healthy }
    networks: [esports-net]

  gateway:
    build: { context: ./gateway/Esports.Gateway, target: dev }
    container_name: esports-gateway
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://0.0.0.0:8080
    ports: ["8080:8080"]         # ← puerta pública para el frontend
    depends_on: [teams, tournaments, matches, ranking]
    networks: [esports-net]

volumes:
  cassandra-data:

networks:
  esports-net:
```

> No se pone `version:` arriba (obsoleto en Compose v2). Usar `docker compose` (con espacio), no `docker-compose`. Si MassTransit no conecta porque RabbitMQ aún inicia, el servicio reintenta solo; el `depends_on: service_healthy` lo minimiza.

## `Dockerfile` por servicio (multi-stage: dev + runtime)

Mismo patrón para los 5 (cambiar solo el nombre del `.dll` en el stage runtime):

```dockerfile
# ---- dev: hot reload con dotnet watch ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /src
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
COPY *.csproj ./
RUN dotnet restore
COPY . ./
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Esports.Teams.Api.dll"]   # ajustar por servicio
```

> Como cada servicio referencia `Esports.Shared`, hay dos opciones para el build: (a) ampliar el `context` a la raíz del repo y ajustar los `COPY` para incluir `shared/`, o (b) empaquetar `Esports.Shared` como dependencia local. El agente debe resolverlo al armar el primer Dockerfile (Fase 1) y dejarlo documentado para que los demás copien el patrón.

## Bootstrap de Cassandra (crear keyspace + tablas al arrancar)

Cada servicio, al iniciar, crea su keyspace y tablas de forma **idempotente** (con reintentos, porque Cassandra puede tardar en aceptar conexiones aun después del healthcheck). Patrón en `Cassandra/SchemaInitializer.cs`, antes de `app.Run()`:

```csharp
// 1) Conectar al cluster SIN keyspace (envuelto en retry con Polly: ~10 intentos, backoff 5s).
// 2) session.Execute("CREATE KEYSPACE IF NOT EXISTS ... RF=1");
// 3) session.ChangeKeyspace("esports_teams");
// 4) session.Execute(cada CREATE TABLE IF NOT EXISTS ...);   // ver docs/02
```

## Cómo correr

```bash
# Mac y Windows: lo mismo, desde la raíz del repo
docker compose up --build
```
La primera vez tarda (baja imágenes + Cassandra arranca ~1-2 min). Cuando los servicios escuchen en `:8080`, está listo. Luego: gateway en `http://localhost:8080`, Swagger en `5001`–`5004`, RabbitMQ en `http://localhost:15672`.

## Troubleshooting

- **"Cassandra unhealthy" / servicios reiniciando al inicio**: normal los primeros ~90s; Cassandra es lenta. El `start_period` lo cubre. Si persiste, subí RAM de Docker Desktop.
- **Puerto ocupado (8080/9042/5672/5001-5004)**: cerrá lo que lo use o cambiá el mapeo `host:contenedor`.
- **Windows: cambios de código no recargan**: confirmá `DOTNET_USE_POLLING_FILE_WATCHER=1` (ya está) y que el repo esté en ruta compartida con Docker (idealmente dentro de WSL2).
- **Windows: scripts/Dockerfile fallan raro**: casi siempre es CRLF. Verificá `.gitattributes` commiteado; volvé a clonar o corré `git add --renormalize .`.
- **Reset total de la base**: `docker compose down -v`.
- **MassTransit pide licencia / error de versión**: te coló la v9. Forzá `Version="8.*"` y `dotnet restore`.
