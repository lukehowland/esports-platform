using global::Cassandra;
using Polly;

namespace Esports.Tournaments.Api.Cassandra;

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
        var keyspace = _config["Cassandra:Keyspace"] ?? "esports_tournaments";

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

            // Tablas base
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS videojuegos (
                    videojuego_id uuid,
                    nombre        text,
                    genero        text,
                    PRIMARY KEY (videojuego_id)
                )"));

            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS organizadores (
                    organizador_id uuid,
                    nombre         text,
                    PRIMARY KEY (organizador_id)
                )"));

            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS torneos (
                    torneo_id          uuid,
                    nombre             text,
                    codigo             text,
                    videojuego_id      uuid,
                    nombre_videojuego  text,
                    organizador_id     uuid,
                    nombre_organizador text,
                    fecha_inicio       timestamp,
                    PRIMARY KEY (torneo_id)
                )"));

            // Q8: videojuegos por género
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS videojuegos_por_genero (
                    genero        text,
                    videojuego_id uuid,
                    nombre        text,
                    PRIMARY KEY ((genero), videojuego_id)
                ) WITH CLUSTERING ORDER BY (videojuego_id ASC)"));

            // Q9: torneos de un videojuego (más reciente primero)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS torneos_por_videojuego (
                    videojuego_id      uuid,
                    fecha_inicio       timestamp,
                    torneo_id          uuid,
                    nombre_torneo      text,
                    nombre_organizador text,
                    PRIMARY KEY ((videojuego_id), fecha_inicio, torneo_id)
                ) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC)"));

            // Q10: lista de organizadores
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS organizadores_lista (
                    bucket         text,
                    organizador_id uuid,
                    nombre         text,
                    PRIMARY KEY ((bucket), organizador_id)
                ) WITH CLUSTERING ORDER BY (organizador_id ASC)"));

            // Q11: torneos de un organizador (más reciente primero)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS torneos_por_organizador (
                    organizador_id    uuid,
                    fecha_inicio      timestamp,
                    torneo_id         uuid,
                    nombre_torneo     text,
                    nombre_videojuego text,
                    PRIMARY KEY ((organizador_id), fecha_inicio, torneo_id)
                ) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC)"));

            // Q12: torneos por fecha de inicio (más reciente primero)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS torneos_por_fecha (
                    bucket            text,
                    fecha_inicio      timestamp,
                    torneo_id         uuid,
                    nombre_torneo     text,
                    nombre_videojuego text,
                    PRIMARY KEY ((bucket), fecha_inicio, torneo_id)
                ) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC)"));

            // Q13: equipos inscritos en un torneo
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS equipos_por_torneo (
                    torneo_id         uuid,
                    equipo_id         uuid,
                    nombre_equipo     text,
                    fecha_inscripcion timestamp,
                    PRIMARY KEY ((torneo_id), equipo_id)
                ) WITH CLUSTERING ORDER BY (equipo_id ASC)"));

            // Q14: torneos en los que participó un equipo (más reciente primero)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS torneos_por_equipo (
                    equipo_id         uuid,
                    fecha_inicio      timestamp,
                    torneo_id         uuid,
                    nombre_torneo     text,
                    nombre_videojuego text,
                    PRIMARY KEY ((equipo_id), fecha_inicio, torneo_id)
                ) WITH CLUSTERING ORDER BY (fecha_inicio DESC, torneo_id ASC)"));

            // Q15: buscar torneo por código único
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS torneo_por_codigo (
                    codigo       text,
                    torneo_id    uuid,
                    nombre       text,
                    fecha_inicio timestamp,
                    PRIMARY KEY (codigo)
                )"));

            // Q20: premios de un torneo (mayor a menor monto)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS premios_por_torneo (
                    torneo_id     uuid,
                    monto         decimal,
                    premio_id     uuid,
                    tipo          text,
                    equipo_id     uuid,
                    nombre_equipo text,
                    PRIMARY KEY ((torneo_id), monto, premio_id)
                ) WITH CLUSTERING ORDER BY (monto DESC, premio_id ASC)"));

            // Q21: premios recibidos por un equipo (mayor a menor monto)
            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS premios_por_equipo (
                    equipo_id     uuid,
                    monto         decimal,
                    premio_id     uuid,
                    torneo_id     uuid,
                    nombre_torneo text,
                    tipo          text,
                    PRIMARY KEY ((equipo_id), monto, premio_id)
                ) WITH CLUSTERING ORDER BY (monto DESC, premio_id ASC)"));

            _logger.LogInformation("Schema for keyspace {Keyspace} initialized successfully.", keyspace);
        });
    }
}
