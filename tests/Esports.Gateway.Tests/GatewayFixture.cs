using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Esports.Gateway.Tests;

public class GatewayFixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public HttpClient Http { get; }

    // Equipos (LoL)
    public Guid T1Id   { get; private set; }
    public Guid FNCId  { get; private set; }
    public Guid G2Id   { get; private set; }
    public Guid FurId  { get; private set; }   // FURIA LoL — no tiene premios

    // Equipos (CS2)
    public Guid NAVIId { get; private set; }
    public Guid FAZEId { get; private set; }

    // Torneos
    public Guid WorldsId    { get; private set; }   // WORLDS25
    public Guid MSIId       { get; private set; }   // MSI26
    public Guid IEMId       { get; private set; }   // IEM-COL26
    public Guid BLASTAus25Id { get; private set; }  // BLAST-AUS25

    // Videojuegos
    public Guid LoLId   { get; private set; }
    public Guid CS2Id   { get; private set; }
    public Guid ValId   { get; private set; }
    public Guid Dota2Id { get; private set; }

    // Organizadores
    public Guid ESLId      { get; private set; }   // ESL FACEIT Group
    public Guid BLASTOrgId { get; private set; }   // BLAST Premier
    public Guid RIOTId     { get; private set; }   // Riot Games

    // Auth
    public string AdminToken { get; private set; } = "";

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

        AdminToken = await LoginAsync("admin", "admin-dev-password");

        T1Id   = await GetTeamIdByTagAsync("T1");
        FNCId  = await GetTeamIdByTagAsync("FNC");
        G2Id   = await GetTeamIdByTagAsync("G2");
        FurId  = await GetTeamIdByTagAsync("FUR");
        NAVIId = await GetTeamIdByTagAsync("NAVI");
        FAZEId = await GetTeamIdByTagAsync("FAZE");

        WorldsId     = await GetTournamentIdByCodeAsync("WORLDS25");
        MSIId        = await GetTournamentIdByCodeAsync("MSI26");
        IEMId        = await GetTournamentIdByCodeAsync("IEM-COL26");
        BLASTAus25Id = await GetTournamentIdByCodeAsync("BLAST-AUS25");

        (LoLId, Dota2Id) = await GetMobaIdsAsync();
        (CS2Id, ValId)   = await GetFpsIdsAsync();
        (ESLId, BLASTOrgId, RIOTId) = await GetOrgIdsAsync();
    }

    public Task DisposeAsync()
    {
        Http.Dispose();
        return Task.CompletedTask;
    }

    // ─── Auth helpers ────────────────────────────────────────────────────────────

    public async Task<string> LoginAsync(string username, string password)
    {
        var body = JsonSerializer.Serialize(new { username, password }, JsonOpts);
        using var r = await Http.PostAsync("/api/auth/login",
            new StringContent(body, Encoding.UTF8, "application/json"));
        r.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    public async Task<HttpResponseMessage> AuthedPost(string path, object body, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Http.SendAsync(req);
    }

    public Task<HttpResponseMessage> AdminPost(string path, object body)
        => AuthedPost(path, body, AdminToken);

    public async Task<HttpResponseMessage> AdminPostJson(string path, string json)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
        return await Http.SendAsync(req);
    }

    // ─── Wait & discovery ────────────────────────────────────────────────────────

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
        throw new Exception("El gateway no respondió después de 2 minutos.");
    }

    private async Task<Guid> GetTeamIdByTagAsync(string tag)
    {
        var r = await Http.GetAsync($"/api/equipos/por-tag/{tag}");
        if (!r.IsSuccessStatusCode)
            throw new Exception($"Equipo con tag '{tag}' no encontrado. ¿Corrió el seeder?");
        var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("equipoId").GetGuid();
    }

    private async Task<Guid> GetTournamentIdByCodeAsync(string code)
    {
        var r = await Http.GetAsync($"/api/torneos/por-codigo/{code}");
        if (!r.IsSuccessStatusCode)
            throw new Exception($"Torneo '{code}' no encontrado. ¿Corrió el seeder?");
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
        if (lol  == default) throw new Exception("League of Legends no encontrado");
        if (dota == default) throw new Exception("Dota 2 no encontrado");
        return (lol, dota);
    }

    private async Task<(Guid cs2Id, Guid valId)> GetFpsIdsAsync()
    {
        var r = await Http.GetAsync("/api/videojuegos/por-genero/FPS");
        r.EnsureSuccessStatusCode();
        var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Guid cs2 = default, val = default;
        foreach (var el in arr.EnumerateArray())
        {
            var nombre = el.GetProperty("nombre").GetString();
            if (nombre == "Counter-Strike 2") cs2 = el.GetProperty("videojuegoId").GetGuid();
            if (nombre == "Valorant")         val = el.GetProperty("videojuegoId").GetGuid();
        }
        if (cs2 == default) throw new Exception("Counter-Strike 2 no encontrado");
        if (val == default) throw new Exception("Valorant no encontrado");
        return (cs2, val);
    }

    private async Task<(Guid eslId, Guid blastId, Guid riotId)> GetOrgIdsAsync()
    {
        var r = await Http.GetAsync("/api/organizadores");
        r.EnsureSuccessStatusCode();
        var arr = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Guid esl = default, blast = default, riot = default;
        foreach (var el in arr.EnumerateArray())
        {
            var nombre = el.GetProperty("nombre").GetString();
            if (nombre == "ESL FACEIT Group") esl   = el.GetProperty("organizadorId").GetGuid();
            if (nombre == "BLAST Premier")    blast = el.GetProperty("organizadorId").GetGuid();
            if (nombre == "Riot Games")       riot  = el.GetProperty("organizadorId").GetGuid();
        }
        if (esl   == default) throw new Exception("ESL FACEIT Group no encontrado");
        if (blast == default) throw new Exception("BLAST Premier no encontrado");
        if (riot  == default) throw new Exception("Riot Games no encontrado");
        return (esl, blast, riot);
    }

    public static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}

[CollectionDefinition("Gateway")]
public class GatewayCollection : ICollectionFixture<GatewayFixture> { }
