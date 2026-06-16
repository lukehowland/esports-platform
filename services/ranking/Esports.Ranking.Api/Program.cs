using Esports.Ranking.Api.Cassandra;
using Esports.Ranking.Api.Consumers;
using Esports.Ranking.Api.Repositories;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Cassandra
builder.Services.AddSingleton<ICassandraSession, CassandraSession>();
builder.Services.AddSingleton<IRankingRepository, RankingRepository>();

// MassTransit: consumir TeamRegisteredToTournament y MatchPlayed
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TeamRegisteredConsumer>();
    x.AddConsumer<MatchPlayedConsumer>();

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
