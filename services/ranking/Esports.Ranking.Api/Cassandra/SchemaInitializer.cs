using global::Cassandra;
using Polly;

namespace Esports.Ranking.Api.Cassandra;

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
        var keyspace = _config["Cassandra:Keyspace"] ?? "esports_ranking";

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

            // Q7 (Fix 1): ranking global de equipos por torneos disputados
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS ranking_equipos_global (
                    bucket        text,
                    equipo_id     uuid,
                    total_torneos counter,
                    PRIMARY KEY ((bucket), equipo_id)
                )"));

            // Q22 (Fix 1): ranking de equipos por victorias
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS ranking_victorias (
                    bucket          text,
                    equipo_id       uuid,
                    total_victorias counter,
                    PRIMARY KEY ((bucket), equipo_id)
                )"));

            // Q23 (Fix 1): jugadores más activos por torneos disputados
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS ranking_jugadores_activos (
                    bucket        text,
                    jugador_id    uuid,
                    total_torneos counter,
                    PRIMARY KEY ((bucket), jugador_id)
                )"));

            // Q23 (Fix 2): meta de jugadores para resolver nickname (tabla plain separada; counter tables no admiten columnas no-counter)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS ranking_jugadores_meta (
                    jugador_id uuid PRIMARY KEY,
                    nickname   text
                )"));

            // Q24: estadísticas de un equipo en un torneo
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS stats_equipo_por_torneo (
                    equipo_id        uuid,
                    torneo_id        uuid,
                    victorias        counter,
                    derrotas         counter,
                    partidas_jugadas counter,
                    PRIMARY KEY ((equipo_id), torneo_id)
                ) WITH CLUSTERING ORDER BY (torneo_id ASC)"));

            _logger.LogInformation("Schema for keyspace {Keyspace} initialized successfully.", keyspace);
        });
    }
}
