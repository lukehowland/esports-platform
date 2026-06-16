using Esports.Auth.Shared;
using Esports.Teams.Api.Cassandra;
using Esports.Teams.Api.Repositories;
using Esports.Teams.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();

// Cassandra
builder.Services.AddSingleton<ICassandraSession, CassandraSession>();

// Repositories
builder.Services.AddSingleton<IEquipoRepository, EquipoRepository>();
builder.Services.AddSingleton<IJugadorRepository, JugadorRepository>();

// Services
builder.Services.AddScoped<IEquipoService, EquipoService>();
builder.Services.AddScoped<IJugadorService, JugadorService>();

// JWT (valida tokens emitidos por el servicio auth)
builder.Services.AddEsportsJwtAuth(builder.Configuration);

// Health check
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Inicializar schema de Cassandra con retry (Polly) antes de aceptar tráfico
var schemaInit = new SchemaInitializer(
    app.Services.GetRequiredService<IConfiguration>(),
    app.Services.GetRequiredService<ILogger<SchemaInitializer>>());
await schemaInit.InitializeAsync();

app.Run();
