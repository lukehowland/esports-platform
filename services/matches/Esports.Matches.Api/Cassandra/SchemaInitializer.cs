using global::Cassandra;
using Polly;

namespace Esports.Matches.Api.Cassandra;

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
        var keyspace = _config["Cassandra:Keyspace"] ?? "esports_matches";

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

            _logger.LogInformation("Initializing schema for keyspace {Keyspace}...", keyspace);

            await session.ExecuteAsync(new SimpleStatement($@"
                CREATE KEYSPACE IF NOT EXISTS {keyspace}
                WITH replication = {{'class': 'SimpleStrategy', 'replication_factor': 1}}"));

            await session.ExecuteAsync(new SimpleStatement($"USE {keyspace}"));

            // Tabla base: partida por id
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS partidas (
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
                )"));

            // Q16: partidas de un torneo (más reciente primero)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS partidas_por_torneo (
                    torneo_id        uuid,
                    fecha            timestamp,
                    partida_id       uuid,
                    nombre_local     text,
                    nombre_visitante text,
                    resultado        text,
                    PRIMARY KEY ((torneo_id), fecha, partida_id)
                ) WITH CLUSTERING ORDER BY (fecha DESC, partida_id ASC)"));

            // Q17: historial de partidas de un equipo (2 filas: local y visitante)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS partidas_por_equipo (
                    equipo_id     uuid,
                    fecha         timestamp,
                    partida_id    uuid,
                    nombre_torneo text,
                    rival         text,
                    resultado     text,
                    PRIMARY KEY ((equipo_id), fecha, partida_id)
                ) WITH CLUSTERING ORDER BY (fecha DESC, partida_id ASC)"));

            // Q18 (Fix 3): partidas jugadas en un día
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS partidas_por_fecha (
                    dia              date,
                    partida_id       uuid,
                    torneo_id        uuid,
                    nombre_local     text,
                    nombre_visitante text,
                    resultado        text,
                    PRIMARY KEY ((dia), partida_id)
                ) WITH CLUSTERING ORDER BY (partida_id ASC)"));

            // Q19 (Fix 2): enfrentamientos directos, bidireccional
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS partidas_por_rivales (
                    equipo_id       uuid,
                    rival_id        uuid,
                    fecha           timestamp,
                    partida_id      uuid,
                    equipo_local_id uuid,
                    resultado       text,
                    PRIMARY KEY ((equipo_id), rival_id, fecha, partida_id)
                ) WITH CLUSTERING ORDER BY (rival_id ASC, fecha DESC, partida_id ASC)"));

            _logger.LogInformation("Schema for keyspace {Keyspace} initialized successfully.", keyspace);
        });
    }
}
