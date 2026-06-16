using Esports.Auth.Api.Cassandra;
using Esports.Auth.Api.Repositories;
using Esports.Auth.Api.Services;
using Esports.Auth.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();

// Cassandra
builder.Services.AddSingleton<ICassandraSession, CassandraSession>();

// Repositories
builder.Services.AddSingleton<IUsuarioRepository, UsuarioRepository>();

// Services
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// JWT (protege /register)
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

// Inicializar schema con retry (Polly) antes de aceptar tráfico
var schemaInit = new SchemaInitializer(
    app.Services.GetRequiredService<IConfiguration>(),
    app.Services.GetRequiredService<ILogger<SchemaInitializer>>(),
    app.Services.GetRequiredService<IPasswordService>());

await schemaInit.InitializeAsync();

app.Run();
