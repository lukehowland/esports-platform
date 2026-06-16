using Cassandra;
using Esports.Auth.Api.Services;
using Polly;

namespace Esports.Auth.Api.Cassandra;

public class SchemaInitializer
{
    private readonly IConfiguration _config;
    private readonly ILogger<SchemaInitializer> _logger;
    private readonly IPasswordService _passwords;

    public SchemaInitializer(IConfiguration config, ILogger<SchemaInitializer> logger, IPasswordService passwords)
    {
        _config = config;
        _logger = logger;
        _passwords = passwords;
    }

    public async Task InitializeAsync()
    {
        var contactPoint = _config["Cassandra:ContactPoints"] ?? "localhost";
        var port = int.Parse(_config["Cassandra:Port"] ?? "9042");
        var keyspace = _config["Cassandra:Keyspace"] ?? "esports_auth";
        var adminUser = _config["Auth:AdminUser"] ?? "admin";
        var adminPassword = _config["Auth:AdminPassword"]
            ?? throw new InvalidOperationException("Falta la configuración Auth:AdminPassword (env Auth__AdminPassword).");

        // Computar hash antes del retry para no rehashear en cada intento
        var adminHash = _passwords.Hash(adminPassword);

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

            await session.ExecuteAsync(new SimpleStatement(@"
                CREATE TABLE IF NOT EXISTS usuarios (
                    username       text,
                    password_hash  text,
                    rol            text,
                    organizador_id uuid,
                    equipo_id      uuid,
                    nombre_display text,
                    PRIMARY KEY (username)
                )"));

            // Sembrar usuario admin idempotentemente (IF NOT EXISTS = LWT seguro)
            var rs = await session.ExecuteAsync(new SimpleStatement(
                $"INSERT INTO {keyspace}.usuarios (username, password_hash, rol, nombre_display) " +
                $"VALUES (?, ?, ?, ?) IF NOT EXISTS",
                adminUser, adminHash, "admin", "Administrador del sistema"));

            var applied = rs.FirstOrDefault()?.GetValue<bool>("[applied]") ?? false;
            if (applied)
                _logger.LogInformation("Usuario admin '{Admin}' sembrado correctamente.", adminUser);
            else
                _logger.LogInformation("Usuario admin '{Admin}' ya existe; se omite el seed.", adminUser);

            _logger.LogInformation("Schema para keyspace {Keyspace} inicializado exitosamente.", keyspace);
        });
    }
}
