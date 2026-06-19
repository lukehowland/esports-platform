using Cassandra;
using Polly;

namespace Esports.Teams.Api.Cassandra;

public class SchemaInitializer
{
    private readonly IConfiguration _config;
    private readonly ILogger<SchemaInitializer> _logger;

    public SchemaInitializer(IConfiguration config, ILogger<SchemaInitializer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var contactPoint = _config["Cassandra:ContactPoints"] ?? "localhost";
        var port = int.Parse(_config["Cassandra:Port"] ?? "9042");
        var keyspace = _config["Cassandra:Keyspace"] ?? "esports_teams";

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 12,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Min(5 * attempt, 30)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.LogWarning("Cassandra not ready (attempt {Attempt}): {Msg}. Retrying in {Delay}s...",
                        attempt, ex.Message, delay.TotalSeconds));

        await retryPolicy.ExecuteAsync(async () =>
        {
            using var cluster = Cluster.Builder()
                .AddContactPoint(contactPoint)
                .WithPort(port)
                .Build();

            using var session = await cluster.ConnectAsync();

            _logger.LogInformation("Connected to Cassandra. Initializing schema for keyspace {Keyspace}...", keyspace);

            await session.ExecuteAsync(new SimpleStatement($@"
                CREATE KEYSPACE IF NOT EXISTS {keyspace}
                WITH replication = {{'class': 'SimpleStrategy', 'replication_factor': 1}}"));

            await session.ExecuteAsync(new SimpleStatement($"USE {keyspace}"));

            // Tabla base: jugador por id
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS jugadores (
                    jugador_id     uuid,
                    codigo         text,
                    nickname       text,
                    nombre         text,
                    pais           text,
                    rol            text,
                    email          text,
                    telefono       text,
                    equipo_id      uuid,
                    fecha_registro timestamp,
                    PRIMARY KEY (jugador_id)
                )"));

            // Tabla base: equipo por id
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS equipos (
                    equipo_id      uuid,
                    nombre         text,
                    tag            text,
                    pais           text,
                    fecha_creacion timestamp,
                    PRIMARY KEY (equipo_id)
                )"));

            // Q1: buscar jugador por nickname exacto
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS jugadores_por_nickname (
                    nickname   text,
                    jugador_id uuid,
                    codigo     text,
                    nombre     text,
                    pais       text,
                    rol        text,
                    email      text,
                    telefono   text,
                    equipo_id  uuid,
                    PRIMARY KEY (nickname)
                )"));

            // Q2: jugadores registrados en un país
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS jugadores_por_pais (
                    pais       text,
                    jugador_id uuid,
                    codigo     text,
                    nickname   text,
                    nombre     text,
                    rol        text,
                    equipo_id  uuid,
                    PRIMARY KEY ((pais), jugador_id)
                ) WITH CLUSTERING ORDER BY (jugador_id ASC)"));

            // Q3: jugadores de un equipo filtrados por país
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS jugadores_por_equipo (
                    equipo_id  uuid,
                    pais       text,
                    jugador_id uuid,
                    codigo     text,
                    nickname   text,
                    nombre     text,
                    rol        text,
                    PRIMARY KEY ((equipo_id), pais, jugador_id)
                ) WITH CLUSTERING ORDER BY (pais ASC, jugador_id ASC)"));

            // Q4: equipos por fecha de creación (más reciente primero)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS equipos_por_fecha (
                    bucket         text,
                    fecha_creacion timestamp,
                    equipo_id      uuid,
                    nombre         text,
                    tag            text,
                    pais           text,
                    PRIMARY KEY ((bucket), fecha_creacion, equipo_id)
                ) WITH CLUSTERING ORDER BY (fecha_creacion DESC, equipo_id ASC)"));

            // Q5: buscar equipo por tag
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS equipos_por_tag (
                    tag       text,
                    equipo_id uuid,
                    nombre    text,
                    pais      text,
                    PRIMARY KEY (tag)
                )"));

            // Q6: integrantes completos de un equipo
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS integrantes_por_equipo (
                    equipo_id  uuid,
                    jugador_id uuid,
                    codigo     text,
                    nickname   text,
                    nombre     text,
                    pais       text,
                    rol        text,
                    PRIMARY KEY ((equipo_id), jugador_id)
                ) WITH CLUSTERING ORDER BY (jugador_id ASC)"));

            // RF-03: lookup de jugador por código legible (J-001), patrón Q5/Q15
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS jugador_por_codigo (
                    codigo     text,
                    jugador_id uuid,
                    nickname   text,
                    nombre     text,
                    pais       text,
                    rol        text,
                    email      text,
                    telefono   text,
                    equipo_id  uuid,
                    PRIMARY KEY (codigo)
                )"));

            // RF-03: membresías jugador↔equipo con validez temporal (N:N en el tiempo).
            // Activa = fecha_hasta IS NULL. La fila nunca se borra: liberar cierra (fecha_hasta).
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS membresias_por_jugador (
                    jugador_id    uuid,
                    fecha_desde   timestamp,
                    equipo_id     uuid,
                    nombre_equipo text,
                    tag_equipo    text,
                    rol           text,
                    fecha_hasta   timestamp,
                    PRIMARY KEY ((jugador_id), fecha_desde, equipo_id)
                ) WITH CLUSTERING ORDER BY (fecha_desde DESC, equipo_id ASC)"));

            // RF-03: secuencia para generar el código J-001 vía LWT (compare-and-set)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS secuencias (
                    nombre text,
                    valor  int,
                    PRIMARY KEY (nombre)
                )"));

            // Volúmenes existentes: agregar la columna 'codigo' de forma idempotente
            // (CREATE ... IF NOT EXISTS no toca tablas ya creadas). Cassandra falla si la
            // columna ya existe; se ignora ese caso.
            var altersIdempotentes = new[]
            {
                "ALTER TABLE jugadores ADD codigo text",
                "ALTER TABLE jugadores_por_nickname ADD codigo text",
                "ALTER TABLE jugadores_por_pais ADD codigo text",
                "ALTER TABLE jugadores_por_equipo ADD codigo text",
                "ALTER TABLE integrantes_por_equipo ADD codigo text",
                // RF-01: email/telefono donde se almacenan (base + Q1 + por-codigo)
                "ALTER TABLE jugadores ADD email text",
                "ALTER TABLE jugadores ADD telefono text",
                "ALTER TABLE jugadores_por_nickname ADD email text",
                "ALTER TABLE jugadores_por_nickname ADD telefono text",
                "ALTER TABLE jugador_por_codigo ADD email text",
                "ALTER TABLE jugador_por_codigo ADD telefono text",
            };
            foreach (var alt in altersIdempotentes)
            {
                try { await session.ExecuteAsync(new SimpleStatement(alt)); }
                catch (Exception ex) { _logger.LogDebug("{Alt} omitido (probablemente ya existe): {Msg}", alt, ex.Message); }
            }

            _logger.LogInformation("Schema for keyspace {Keyspace} initialized successfully.", keyspace);
        });
    }
}
