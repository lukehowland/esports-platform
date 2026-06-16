using System.Text;
using System.Text.Json;

var gatewayUrl = Environment.GetEnvironmentVariable("GatewayUrl") ?? "http://localhost:8080";
var http = new HttpClient { BaseAddress = new Uri(gatewayUrl) };
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

Console.WriteLine($"Seeder conectando a {gatewayUrl}...");

// Esperar hasta que el gateway responda
for (int i = 0; i < 30; i++)
{
    try { await http.GetAsync("/health"); break; }
    catch { Console.WriteLine($"Gateway no disponible, reintentando ({i + 1}/30)..."); await Task.Delay(3000); }
}

static StringContent Json(object obj) =>
    new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

static async Task<Guid> Post(HttpClient http, string path, object body)
{
    var resp = await http.PostAsync(path, Json(body));
    var raw = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new Exception($"POST {path} => {resp.StatusCode}: {raw}");
    using var doc = JsonDocument.Parse(raw);
    // primer campo guid del response
    foreach (var prop in doc.RootElement.EnumerateObject())
        if (prop.Value.ValueKind == JsonValueKind.String &&
            Guid.TryParse(prop.Value.GetString(), out var id))
            return id;
    throw new Exception($"No se pudo extraer GUID de: {raw}");
}

Console.WriteLine("\n=== Creando videojuegos ===");
var lolId = await Post(http, "/api/videojuegos", new { nombre = "League of Legends", genero = "MOBA" });
var valId = await Post(http, "/api/videojuegos", new { nombre = "Valorant", genero = "FPS" });
var cs2Id = await Post(http, "/api/videojuegos", new { nombre = "Counter-Strike 2", genero = "FPS" });
var dota2Id = await Post(http, "/api/videojuegos", new { nombre = "Dota 2", genero = "MOBA" });
Console.WriteLine($"LoL={lolId}, Valorant={valId}, CS2={cs2Id}, Dota2={dota2Id}");

Console.WriteLine("\n=== Creando organizadores ===");
var eslId = await Post(http, "/api/organizadores", new { nombre = "ESL Gaming" });
var pglId = await Post(http, "/api/organizadores", new { nombre = "PGL" });
var blastId = await Post(http, "/api/organizadores", new { nombre = "BLAST Premier" });
Console.WriteLine($"ESL={eslId}, PGL={pglId}, BLAST={blastId}");

Console.WriteLine("\n=== Creando equipos ===");
var t1Id = await Post(http, "/api/equipos", new { nombre = "T1", tag = "T1", pais = "KR" });
var drxId = await Post(http, "/api/equipos", new { nombre = "DRX", tag = "DRX", pais = "KR" });
var fnId = await Post(http, "/api/equipos", new { nombre = "Fnatic", tag = "FNC", pais = "GB" });
var c9Id = await Post(http, "/api/equipos", new { nombre = "Cloud9", tag = "C9", pais = "US" });
var naviId = await Post(http, "/api/equipos", new { nombre = "Natus Vincere", tag = "NaVi", pais = "UA" });
var fazeId = await Post(http, "/api/equipos", new { nombre = "FaZe Clan", tag = "FaZe", pais = "EU" });
var liqId = await Post(http, "/api/equipos", new { nombre = "Team Liquid", tag = "TL", pais = "US" });
var g2Id = await Post(http, "/api/equipos", new { nombre = "G2 Esports", tag = "G2", pais = "ES" });
Console.WriteLine($"T1={t1Id}, DRX={drxId}, FNC={fnId}, C9={c9Id}, NaVi={naviId}, FaZe={fazeId}, TL={liqId}, G2={g2Id}");

Console.WriteLine("\n=== Agregando jugadores ===");
// T1
await Post(http, $"/api/equipos/{t1Id}/jugadores", new { nickname = "Faker", nombre = "Lee Sang-hyeok", pais = "KR", rol = "MID" });
await Post(http, $"/api/equipos/{t1Id}/jugadores", new { nickname = "Gumayusi", nombre = "Lee Min-hyeong", pais = "KR", rol = "ADC" });
await Post(http, $"/api/equipos/{t1Id}/jugadores", new { nickname = "Zeus", nombre = "Choi Woo-je", pais = "KR", rol = "TOP" });
// DRX
await Post(http, $"/api/equipos/{drxId}/jugadores", new { nickname = "Zeka", nombre = "Kim Geon-woo", pais = "KR", rol = "MID" });
await Post(http, $"/api/equipos/{drxId}/jugadores", new { nickname = "Deft", nombre = "Kim Hyuk-kyu", pais = "KR", rol = "ADC" });
// FNC
await Post(http, $"/api/equipos/{fnId}/jugadores", new { nickname = "Humanoid", nombre = "Marek Brazda", pais = "CZ", rol = "MID" });
await Post(http, $"/api/equipos/{fnId}/jugadores", new { nickname = "Upset", nombre = "Elias Lipp", pais = "DE", rol = "ADC" });
// NaVi
await Post(http, $"/api/equipos/{naviId}/jugadores", new { nickname = "s1mple", nombre = "Oleksandr Kostylev", pais = "UA", rol = "SNIPER" });
await Post(http, $"/api/equipos/{naviId}/jugadores", new { nickname = "electronic", nombre = "Denis Sharipov", pais = "RU", rol = "RIFLER" });
// FaZe
await Post(http, $"/api/equipos/{fazeId}/jugadores", new { nickname = "karrigan", nombre = "Finn Andersen", pais = "DK", rol = "IGL" });
// C9
await Post(http, $"/api/equipos/{c9Id}/jugadores", new { nickname = "Xeppaa", nombre = "Erick Bach", pais = "US", rol = "DUELIST" });
// TL
await Post(http, $"/api/equipos/{liqId}/jugadores", new { nickname = "nAts", nombre = "Ayaz Akhmetshin", pais = "RU", rol = "SENTINEL" });
// G2
await Post(http, $"/api/equipos/{g2Id}/jugadores", new { nickname = "Mixwell", nombre = "Oscar Cañellas", pais = "ES", rol = "DUELIST" });
Console.WriteLine("Jugadores creados.");

Console.WriteLine("\n=== Creando torneos ===");
var worlds25Id = await Post(http, "/api/torneos", new {
    nombre = "Worlds 2025", codigo = "WORLDS25",
    videojuegoId = lolId, organizadorId = eslId,
    fechaInicio = "2025-10-15T00:00:00Z"
});
var msi26Id = await Post(http, "/api/torneos", new {
    nombre = "MSI 2026", codigo = "MSI26",
    videojuegoId = lolId, organizadorId = pglId,
    fechaInicio = "2026-05-01T00:00:00Z"
});
var blastSpId = await Post(http, "/api/torneos", new {
    nombre = "BLAST Spring 2026", codigo = "BLAST-SPR26",
    videojuegoId = valId, organizadorId = blastId,
    fechaInicio = "2026-03-10T00:00:00Z"
});
var csMajorId = await Post(http, "/api/torneos", new {
    nombre = "CS2 Major Copenhagen", codigo = "CS2MAJOR26",
    videojuegoId = cs2Id, organizadorId = pglId,
    fechaInicio = "2026-04-01T00:00:00Z"
});
var dotaId = await Post(http, "/api/torneos", new {
    nombre = "The International 2025", codigo = "TI25",
    videojuegoId = dota2Id, organizadorId = eslId,
    fechaInicio = "2025-08-20T00:00:00Z"
});
Console.WriteLine($"Worlds={worlds25Id}, MSI={msi26Id}, BLAST={blastSpId}, CSMajor={csMajorId}, TI={dotaId}");

Console.WriteLine("\n=== Inscripciones ===");
// Worlds 2025: T1 y DRX
await http.PostAsync($"/api/torneos/{worlds25Id}/inscripciones", Json(new { equipoId = t1Id }));
await http.PostAsync($"/api/torneos/{worlds25Id}/inscripciones", Json(new { equipoId = drxId }));
await http.PostAsync($"/api/torneos/{worlds25Id}/inscripciones", Json(new { equipoId = fnId }));
// MSI 2026: T1, FNC, G2
await http.PostAsync($"/api/torneos/{msi26Id}/inscripciones", Json(new { equipoId = t1Id }));
await http.PostAsync($"/api/torneos/{msi26Id}/inscripciones", Json(new { equipoId = fnId }));
await http.PostAsync($"/api/torneos/{msi26Id}/inscripciones", Json(new { equipoId = g2Id }));
// BLAST: C9, TL, FNC
await http.PostAsync($"/api/torneos/{blastSpId}/inscripciones", Json(new { equipoId = c9Id }));
await http.PostAsync($"/api/torneos/{blastSpId}/inscripciones", Json(new { equipoId = liqId }));
await http.PostAsync($"/api/torneos/{blastSpId}/inscripciones", Json(new { equipoId = fnId }));
// CS Major: NaVi, FaZe, C9
await http.PostAsync($"/api/torneos/{csMajorId}/inscripciones", Json(new { equipoId = naviId }));
await http.PostAsync($"/api/torneos/{csMajorId}/inscripciones", Json(new { equipoId = fazeId }));
await http.PostAsync($"/api/torneos/{csMajorId}/inscripciones", Json(new { equipoId = c9Id }));
Console.WriteLine("Inscripciones realizadas.");

Console.WriteLine("\n=== Premios ===");
await http.PostAsync($"/api/torneos/{worlds25Id}/premios", Json(new { monto = 500000m, tipo = "Primer lugar", equipoId = t1Id }));
await http.PostAsync($"/api/torneos/{worlds25Id}/premios", Json(new { monto = 200000m, tipo = "Segundo lugar", equipoId = drxId }));
await http.PostAsync($"/api/torneos/{csMajorId}/premios", Json(new { monto = 250000m, tipo = "Campeón", equipoId = naviId }));
Console.WriteLine("Premios asignados.");

Console.WriteLine("\n=== Partidas ===");
async Task Partida(Guid trn, string nomTrn, Guid loc, string nomLoc, Guid vis, string nomVis, Guid ganador, string res, string fecha) =>
    await http.PostAsync("/api/partidas", Json(new {
        torneoId = trn, nombreTorneo = nomTrn,
        equipoLocalId = loc, nombreLocal = nomLoc,
        equipoVisitanteId = vis, nombreVisitante = nomVis,
        equipoGanadorId = ganador, resultado = res, fecha
    }));

// Worlds 2025
await Partida(worlds25Id, "Worlds 2025", t1Id, "T1", drxId, "DRX", t1Id, "T1 WIN 3-1", "2025-10-15T14:00:00Z");
await Partida(worlds25Id, "Worlds 2025", drxId, "DRX", fnId, "Fnatic", drxId, "DRX WIN 2-0", "2025-10-16T14:00:00Z");
await Partida(worlds25Id, "Worlds 2025", t1Id, "T1", fnId, "Fnatic", t1Id, "T1 WIN 3-0", "2025-10-17T14:00:00Z");
await Partida(worlds25Id, "Worlds 2025", t1Id, "T1", drxId, "DRX", drxId, "DRX WIN 3-2 (Remontada)", "2025-10-20T14:00:00Z");
await Partida(worlds25Id, "Worlds 2025", t1Id, "T1", drxId, "DRX", t1Id, "T1 WIN 3-1 (Final)", "2025-10-22T18:00:00Z");
// MSI 2026
await Partida(msi26Id, "MSI 2026", t1Id, "T1", fnId, "Fnatic", t1Id, "T1 WIN 2-0", "2026-05-03T18:00:00Z");
await Partida(msi26Id, "MSI 2026", fnId, "Fnatic", g2Id, "G2 Esports", g2Id, "G2 WIN 2-1", "2026-05-04T18:00:00Z");
// CS Major
await Partida(csMajorId, "CS2 Major Copenhagen", naviId, "NaVi", fazeId, "FaZe Clan", naviId, "16-12", "2026-04-10T16:00:00Z");
await Partida(csMajorId, "CS2 Major Copenhagen", fazeId, "FaZe Clan", c9Id, "Cloud9", fazeId, "16-8", "2026-04-11T16:00:00Z");
await Partida(csMajorId, "CS2 Major Copenhagen", naviId, "NaVi", c9Id, "Cloud9", naviId, "16-10", "2026-04-12T16:00:00Z");
// BLAST
await Partida(blastSpId, "BLAST Spring 2026", c9Id, "Cloud9", liqId, "Team Liquid", c9Id, "2-0", "2026-03-12T20:00:00Z");
await Partida(blastSpId, "BLAST Spring 2026", liqId, "Team Liquid", fnId, "Fnatic", liqId, "2-1", "2026-03-13T20:00:00Z");
Console.WriteLine("Partidas registradas.");

Console.WriteLine("\n=== ¡Seeder completado con éxito! ===");
Console.WriteLine("Datos de ejemplo cargados:");
Console.WriteLine("  - 4 videojuegos (LoL, Valorant, CS2, Dota 2)");
Console.WriteLine("  - 3 organizadores");
Console.WriteLine("  - 8 equipos con sus jugadores");
Console.WriteLine("  - 5 torneos");
Console.WriteLine("  - 12 inscripciones");
Console.WriteLine("  - 3 premios");
Console.WriteLine("  - 12 partidas");
