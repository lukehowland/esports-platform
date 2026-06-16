using Esports.Matches.Api.Cassandra;
using Esports.Matches.Api.Repositories;
using Esports.Matches.Api.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Cassandra
builder.Services.AddSingleton<ICassandraSession, CassandraSession>();
builder.Services.AddSingleton<IPartidaRepository, PartidaRepository>();

// Services
builder.Services.AddScoped<IPartidaService, PartidaService>();

// MassTransit: publisher de MatchPlayed
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

app.MapControllers();
app.MapHealthChecks("/health");

// Inicializar schema
var logger = app.Services.GetRequiredService<ILogger<SchemaInitializer>>();
var schemaInit = new SchemaInitializer(config, logger);
await schemaInit.InitializeAsync();

app.Run();
