using System.Text.Json;
using Xunit;

namespace Esports.Gateway.Tests;

/// <summary>
/// Fixture compartida entre todas las clases de test.
/// Espera al gateway, descubre los IDs del seeder y expone el HttpClient.
/// </summary>
public class GatewayFixture : IAsyncLifetime
{
    public HttpClient Http { get; }

    // Equipos (descubiertos por tag estable del seeder)
    public Guid T1Id { get; private set; }
    public Guid DRXId { get; private set; }
    public Guid NaViId { get; private set; }
    public Guid FnaticId { get; private set; }
    public Guid FaZeId { get; private set; }
    public Guid C9Id { get; private set; }
    public Guid G2Id { get; private set; }
    public Guid TLId { get; private set; }

    // Torneos (descubiertos por código estable del seeder)
    public Guid WorldsId { get; private set; }
    public Guid MSIId { get; private set; }
    public Guid CSMajorId { get; private set; }
    public Guid BLASTId { get; private set; }
    public Guid TIId { get; private set; }

    // Videojuegos y organizadores
    public Guid LoLId { get; private set; }
    public Guid Dota2Id { get; private set; }
    public Guid ESLId { get; private set; }
    public Guid PGLId { get; private set; }

    public GatewayFixture()
    {
        var baseUrl = Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://localhost:8080";
        Http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task InitializeAsync()
    {
        await WaitForGatewayAsync();

        T1Id      = await GetTeamIdByTagAsync("T1");
        DRXId     = await GetTeamIdByTagAsync("DRX");
        NaViId    = await GetTeamIdByTagAsync("NaVi");
        FnaticId  = await GetTeamIdByTagAsync("FNC");
        FaZeId    = await GetTeamIdByTagAsync("FaZe");
        C9Id      = await GetTeamIdByTagAsync("C9");
        G2Id      = await GetTeamIdByTagAsync("G2");
        TLId      = await GetTeamIdByTagAsync("TL");

        WorldsId  = await GetTournamentIdByCodeAsync("WORLDS25");
        MSIId     = await GetTournamentIdByCodeAsync("MSI26");
        CSMajorId = await GetTournamentIdByCodeAsync("CS2MAJOR26");
        BLASTId   = await GetTournamentIdByCodeAsync("BLAST-SPR26");
        TIId      = await GetTournamentIdByCodeAsync("TI25");

        (LoLId, Dota2Id) = await GetMobaIdsAsync();
        (ESLId, PGLId)   = await GetOrgIdsAsync();
    }

    public Task DisposeAsync()
    {
        Http.Dispose();
        return Task.CompletedTask;
    }

    private async Task WaitForGatewayAsync()
    {
        for (int i = 0; i < 40; i++)
        {
            try
            {
                var r = await Http.GetAsync("/health");
                if (r.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(3000);
        }
        throw new Exception("El gateway no respondió después de 2 minutos. Asegúrate de que el stack esté levantado con 'docker compose up'.");
    }

    private async Task<Guid> GetTeamIdByTagAsync(string tag)
    {
        var r = await Http.GetAsync($"/api/equipos/por-tag/{tag}");
        if (!r.IsSuccessStatusCode)
            throw new Exception($"Equipo con tag '{tag}' no encontrado. Ejecuta el seeder primero: docker compose run --rm seeder");
        var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("equipoId").GetGuid();
    }

    private async Task<Guid> GetTournamentIdByCodeAsync(string code)
    {
        var r = await Http.GetAsync($"/api/torneos/por-codigo/{code}");
        if (!r.IsSuccessStatusCode)
            throw new Exception($"Torneo '{code}' no encontrado. Ejecuta el seeder primero: docker compose run --rm seeder");
        var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("torneoId").GetGuid();
    }

    private async Task<(Guid lolId, Guid dota2Id)> GetMobaIdsAsync()
    {
        var r = await Http.GetAsync("/api/videojuegos/por-genero/MOBA");
        r.EnsureSuccessStatusCode();
        var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Guid lol = default, dota = default;
        foreach (var el in arr.EnumerateArray())
        {
            var nombre = el.GetProperty("nombre").GetString();
            if (nombre == "League of Legends") lol  = el.GetProperty("videojuegoId").GetGuid();
            if (nombre == "Dota 2")            dota = el.GetProperty("videojuegoId").GetGuid();
        }
        if (lol == default)  throw new Exception("League of Legends no encontrado");
        if (dota == default) throw new Exception("Dota 2 no encontrado");
        return (lol, dota);
    }

    private async Task<(Guid eslId, Guid pglId)> GetOrgIdsAsync()
    {
        var r = await Http.GetAsync("/api/organizadores");
        r.EnsureSuccessStatusCode();
        var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Guid esl = default, pgl = default;
        foreach (var el in arr.EnumerateArray())
        {
            var nombre = el.GetProperty("nombre").GetString();
            if (nombre == "ESL Gaming") esl = el.GetProperty("organizadorId").GetGuid();
            if (nombre == "PGL")        pgl = el.GetProperty("organizadorId").GetGuid();
        }
        if (esl == default) throw new Exception("ESL Gaming no encontrado");
        if (pgl == default) throw new Exception("PGL no encontrado");
        return (esl, pgl);
    }

    public static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}

[CollectionDefinition("Gateway")]
public class GatewayCollection : ICollectionFixture<GatewayFixture> { }
