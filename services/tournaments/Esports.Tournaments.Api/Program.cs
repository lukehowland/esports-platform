using Esports.Auth.Shared;
using Esports.Tournaments.Api.Cassandra;
using Esports.Tournaments.Api.HttpClients;
using Esports.Tournaments.Api.Repositories;
using Esports.Tournaments.Api.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Cassandra
builder.Services.AddSingleton<ICassandraSession, CassandraSession>();
builder.Services.AddSingleton<IVideojuegoRepository, VideojuegoRepository>();
builder.Services.AddSingleton<IOrganizadorRepository, OrganizadorRepository>();
builder.Services.AddSingleton<ITorneoRepository, TorneoRepository>();
builder.Services.AddSingleton<IInscripcionRepository, InscripcionRepository>();
builder.Services.AddSingleton<IPremioRepository, PremioRepository>();

// Services
builder.Services.AddScoped<IVideojuegoService, VideojuegoService>();
builder.Services.AddScoped<IOrganizadorService, OrganizadorService>();
builder.Services.AddScoped<ITorneoService, TorneoService>();
builder.Services.AddScoped<IInscripcionService, InscripcionService>();
builder.Services.AddScoped<IPremioService, PremioService>();

// HTTP client tipado para teams
builder.Services.AddHttpClient<TeamsClient>(c =>
    c.BaseAddress = new Uri(config["Services:Teams"] ?? "http://teams:8080"));

// JWT (valida tokens emitidos por el servicio auth)
builder.Services.AddEsportsJwtAuth(builder.Configuration);

// MassTransit con RabbitMQ (solo publisher — tournaments no consume eventos)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(config["RabbitMq:Host"] ?? "localhost", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Inicializar schema antes de arrancar
var logger = app.Services.GetRequiredService<ILogger<SchemaInitializer>>();
var schemaInit = new SchemaInitializer(config, logger);
await schemaInit.InitializeAsync();

app.Run();
