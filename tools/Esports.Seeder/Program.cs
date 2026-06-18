using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

await SeederApp.RunAsync();

internal static class SeederApp
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task RunAsync()
    {
        var gatewayUrl = Environment.GetEnvironmentVariable("GatewayUrl") ?? "http://localhost:8080";
        var adminUser = Environment.GetEnvironmentVariable("Auth__AdminUser") ?? "admin";
        var adminPassword = Environment.GetEnvironmentVariable("Auth__AdminPassword") ?? "admin-dev-password";

        using var http = new HttpClient
        {
            BaseAddress = new Uri(gatewayUrl),
            Timeout = TimeSpan.FromSeconds(45)
        };

        Console.WriteLine($"Seeder conectando a {gatewayUrl}...");
        await WaitForGatewayAsync(http);

        // Autenticarse como admin antes de cualquier mutación
        var token = await LoginAdminAsync(http, adminUser, adminPassword);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Console.WriteLine("Login admin exitoso. Usando token JWT para todas las mutaciones.");

        var games = new Dictionary<string, VideojuegoResponse>();
        foreach (var game in SeedData.Games)
            games[game.Code] = await EnsureVideojuegoAsync(http, game);

        var organizers = new Dictionary<string, OrganizadorResponse>();
        foreach (var organizer in SeedData.Organizers)
            organizers[organizer.Code] = await EnsureOrganizadorAsync(http, organizer);

        var teams = new Dictionary<string, EquipoResponse>();
        foreach (var team in SeedData.Teams)
        {
            var createdTeam = await EnsureEquipoAsync(http, team);
            teams[team.Key] = createdTeam;

            foreach (var player in SeedData.BuildRoster(team))
                await EnsureJugadorAsync(http, createdTeam.EquipoId, player);
        }

        // RF-03: traspaso de ejemplo para que el historial de equipos no esté vacío en la demo.
        await SeedDemoTransferAsync(http, teams);

        var tournaments = new Dictionary<string, TorneoResponse>();
        foreach (var tournament in SeedData.Tournaments)
        {
            var createdTournament = await EnsureTorneoAsync(http, tournament, games, organizers);
            tournaments[tournament.Key] = createdTournament;

            foreach (var teamKey in tournament.TeamKeys)
                await EnsureInscripcionAsync(http, createdTournament, teams[teamKey]);

            if (tournament.GeneratePrizePack)
                await EnsurePrizePackAsync(http, createdTournament, tournament, teams);

            if (tournament.GenerateMatches)
                await EnsureMatchesAsync(http, createdTournament, tournament, teams);
        }

        await WaitForRankingAsync(http);
        await PrintSummaryAsync(http);

        // Registrar usuarios demo por rol para el frontend
        await RegisterDemoUsersAsync(http, organizers, teams);

        Console.WriteLine();
        Console.WriteLine("=== Seeder completado con exito ===");
    }

    private static async Task WaitForGatewayAsync(HttpClient http)
    {
        for (var i = 1; i <= 60; i++)
        {
            try
            {
                using var response = await http.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Gateway saludable.");
                    return;
                }

                Console.WriteLine($"Gateway respondio {response.StatusCode}; reintento {i}/60...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gateway no disponible ({ex.Message}); reintento {i}/60...");
            }

            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        throw new TimeoutException("El gateway no estuvo disponible para ejecutar el seed.");
    }

    private static async Task<string> LoginAdminAsync(HttpClient http, string username, string password)
    {
        using var response = await http.PostAsync("/api/auth/login", ToJson(new { username, password }));
        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login admin falló ({(int)response.StatusCode}): {raw}");

        var login = JsonSerializer.Deserialize<LoginResponse>(raw, JsonOptions)
            ?? throw new InvalidOperationException("Login admin no devolvio JSON valido.");
        return login.Token;
    }

    private static async Task RegisterDemoUsersAsync(
        HttpClient http,
        IReadOnlyDictionary<string, OrganizadorResponse> organizers,
        IReadOnlyDictionary<string, EquipoResponse> teams)
    {
        Console.WriteLine();
        Console.WriteLine("Registrando usuarios demo por rol...");

        foreach (var (code, org) in organizers)
        {
            await RegisterUserAsync(http,
                username: $"org_{code.ToLowerInvariant()}",
                password: "OrgDemo2024",
                rol: "organizador",
                nombreDisplay: org.Nombre,
                organizadorId: org.OrganizadorId);
        }

        foreach (var (_, team) in teams)
        {
            await RegisterUserAsync(http,
                username: $"cap_{team.Tag.ToLowerInvariant()}",
                password: "CapDemo2024",
                rol: "capitan",
                nombreDisplay: $"Capitan {team.Nombre}",
                equipoId: team.EquipoId);
        }

        await RegisterUserAsync(http,
            username: "fan_demo",
            password: "FanDemo2024",
            rol: "fan",
            nombreDisplay: "Fan Demo");

        Console.WriteLine("Usuarios demo registrados.");
        Console.WriteLine("  Admin:         admin           / admin-dev-password");
        Console.WriteLine("  Organizadores: org_<code>      / OrgDemo2024  (ej: org_riot, org_esl, org_vct)");
        Console.WriteLine("  Capitanes:     cap_<tag>       / CapDemo2024  (ej: cap_navi, cap_t1, cap_g2)");
        Console.WriteLine("  Fan:           fan_demo        / FanDemo2024");
    }

    private static async Task RegisterUserAsync(HttpClient http, string username, string password,
        string rol, string nombreDisplay, Guid? organizadorId = null, Guid? equipoId = null)
    {
        var body = new
        {
            username,
            password,
            rol,
            organizadorId,
            equipoId,
            nombreDisplay
        };

        using var response = await http.PostAsync("/api/auth/register", ToJson(body));

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            Console.WriteLine($"  Usuario '{username}' ya existe (omitido).");
            return;
        }

        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Register '{username}' => {(int)response.StatusCode}: {raw}");

        Console.WriteLine($"  Usuario '{username}' ({rol}) registrado.");
    }

    private static async Task<VideojuegoResponse> EnsureVideojuegoAsync(HttpClient http, GameSeed seed)
    {
        var genre = NormalizeCode(seed.Genero);
        var existing = await GetOrEmptyAsync<VideojuegoPorGeneroResponse>(http, $"/api/videojuegos/por-genero/{Uri.EscapeDataString(genre)}");
        var match = existing.FirstOrDefault(v => Same(v.Nombre, seed.Nombre));
        if (match is not null)
        {
            Console.WriteLine($"Videojuego existente: {seed.Nombre}");
            return new VideojuegoResponse(match.VideojuegoId, match.Nombre, genre);
        }

        var created = await PostAsync<VideojuegoResponse>(http, "/api/videojuegos", new
        {
            nombre = seed.Nombre,
            genero = genre
        });
        Console.WriteLine($"Videojuego creado: {created.Nombre}");
        return created;
    }

    private static async Task<OrganizadorResponse> EnsureOrganizadorAsync(HttpClient http, OrganizerSeed seed)
    {
        var existing = await GetOrEmptyAsync<OrganizadorResponse>(http, "/api/organizadores");
        var match = existing.FirstOrDefault(o => Same(o.Nombre, seed.Nombre));
        if (match is not null)
        {
            Console.WriteLine($"Organizador existente: {seed.Nombre}");
            return match;
        }

        var created = await PostAsync<OrganizadorResponse>(http, "/api/organizadores", new
        {
            nombre = seed.Nombre
        });
        Console.WriteLine($"Organizador creado: {created.Nombre}");
        return created;
    }

    private static async Task<EquipoResponse> EnsureEquipoAsync(HttpClient http, TeamSeed seed)
    {
        var tag = NormalizeCode(seed.Tag);
        var existing = await GetOptionalAsync<EquipoResponse>(http, $"/api/equipos/por-tag/{Uri.EscapeDataString(tag)}");
        if (existing is not null)
        {
            Console.WriteLine($"Equipo existente: [{tag}] {existing.Nombre}");
            return existing;
        }

        var created = await PostAsync<EquipoResponse>(http, "/api/equipos", new
        {
            nombre = seed.Nombre,
            tag,
            pais = NormalizeCode(seed.Pais)
        });
        Console.WriteLine($"Equipo creado: [{created.Tag}] {created.Nombre}");
        return created;
    }

    private static async Task EnsureJugadorAsync(HttpClient http, Guid equipoId, PlayerSeed seed)
    {
        var nickname = seed.Nickname.Trim();
        var existing = await GetOptionalAsync<JugadorResponse>(http, $"/api/jugadores/por-nickname/{Uri.EscapeDataString(nickname)}");
        if (existing is not null)
            return;

        var created = await PostAsync<JugadorResponse>(http, $"/api/equipos/{equipoId}/jugadores", new
        {
            nickname,
            nombre = seed.Nombre.Trim(),
            pais = NormalizeCode(seed.Pais),
            rol = seed.Rol.Trim()
        });
        Console.WriteLine($"  Jugador creado: {created.Nickname}");
    }

    // RF-03: siembra un traspaso real (KC -> FNC, ambos LoL) para que el historial de
    // equipos del jugador tenga dos entradas en la demo. No toca rosters fijados por tests.
    private static async Task SeedDemoTransferAsync(HttpClient http, IReadOnlyDictionary<string, EquipoResponse> teams)
    {
        if (!teams.TryGetValue("LOL_KC", out var origen) || !teams.TryGetValue("LOL_FNC", out var destino))
            return;

        const string nick = "Wanderer";
        var jugador = await GetOptionalAsync<JugadorResponse>(http, $"/api/jugadores/por-nickname/{Uri.EscapeDataString(nick)}");
        if (jugador is null)
        {
            jugador = await PostAsync<JugadorResponse>(http, $"/api/equipos/{origen.EquipoId}/jugadores", new
            {
                nickname = nick,
                nombre = "Alex Wanderer",
                pais = "FR",
                rol = "JUNGLE"
            });
            Console.WriteLine($"  Jugador demo de traspaso creado: {jugador.Nickname} ({jugador.Codigo}) en {origen.Tag}");
        }

        var membresias = await GetOrEmptyAsync<MembresiaResponse>(http, $"/api/jugadores/{jugador.JugadorId}/membresias");
        if (membresias.Count >= 2)
        {
            Console.WriteLine("  Traspaso demo ya aplicado (omitido).");
            return;
        }

        // Admin: asignar a un jugador con equipo activo = traspaso atómico (baja + alta).
        await PostNoBodyAsync(http, $"/api/jugadores/{jugador.JugadorId}/asignar", new { equipoDestinoId = destino.EquipoId });
        Console.WriteLine($"  Traspaso demo aplicado: {jugador.Nickname} {origen.Tag} -> {destino.Tag}");
    }

    private static async Task<TorneoResponse> EnsureTorneoAsync(
        HttpClient http,
        TournamentSeed seed,
        IReadOnlyDictionary<string, VideojuegoResponse> games,
        IReadOnlyDictionary<string, OrganizadorResponse> organizers)
    {
        var code = NormalizeCode(seed.Codigo);
        var existing = await GetOptionalAsync<TorneoPorCodigoResponse>(http, $"/api/torneos/por-codigo/{Uri.EscapeDataString(code)}");
        if (existing is not null)
        {
            var full = await GetRequiredAsync<TorneoResponse>(http, $"/api/torneos/{existing.TorneoId}");
            Console.WriteLine($"Torneo existente: {full.Codigo} - {full.Nombre}");
            return full;
        }

        var created = await PostAsync<TorneoResponse>(http, "/api/torneos", new
        {
            nombre = seed.Nombre,
            codigo = code,
            videojuegoId = games[seed.GameCode].VideojuegoId,
            organizadorId = organizers[seed.OrganizerCode].OrganizadorId,
            fechaInicio = ParseUtc(seed.FechaInicio)
        });
        Console.WriteLine($"Torneo creado: {created.Codigo} - {created.Nombre}");
        return created;
    }

    private static async Task EnsureInscripcionAsync(HttpClient http, TorneoResponse torneo, EquipoResponse equipo)
    {
        var existing = await GetOrEmptyAsync<EquipoPorTorneoResponse>(http, $"/api/torneos/{torneo.TorneoId}/equipos");
        if (existing.Any(e => e.EquipoId == equipo.EquipoId))
            return;

        await PostNoBodyAsync(http, $"/api/torneos/{torneo.TorneoId}/inscripciones", new
        {
            equipoId = equipo.EquipoId
        });
        Console.WriteLine($"  Inscrito: [{equipo.Tag}] en {torneo.Codigo}");
    }

    private static async Task EnsurePrizePackAsync(
        HttpClient http,
        TorneoResponse torneo,
        TournamentSeed seed,
        IReadOnlyDictionary<string, EquipoResponse> teams)
    {
        var podium = seed.TeamKeys.Take(4).Select(k => teams[k]).ToArray();
        var prizes = new[]
        {
            new PrizeSeed("Campeon", seed.BasePrize, podium[0].EquipoId),
            new PrizeSeed("Subcampeon", Math.Round(seed.BasePrize * 0.45m, 2), podium[1].EquipoId),
            new PrizeSeed("Semifinalista", Math.Round(seed.BasePrize * 0.20m, 2), podium[2].EquipoId),
            new PrizeSeed("MVP del torneo", Math.Round(seed.BasePrize * 0.08m, 2), podium[3].EquipoId)
        };

        var existing = await GetOrEmptyAsync<PremioResponse>(http, $"/api/torneos/{torneo.TorneoId}/premios");
        foreach (var prize in prizes)
        {
            if (existing.Any(p => Same(p.Tipo, prize.Tipo) && p.EquipoId == prize.EquipoId && p.Monto == prize.Monto))
                continue;

            await PostAsync<PremioResponse>(http, $"/api/torneos/{torneo.TorneoId}/premios", new
            {
                monto = prize.Monto,
                tipo = prize.Tipo,
                equipoId = prize.EquipoId
            });
            Console.WriteLine($"  Premio creado: {torneo.Codigo} - {prize.Tipo}");
        }
    }

    private static async Task EnsureMatchesAsync(
        HttpClient http,
        TorneoResponse torneo,
        TournamentSeed seed,
        IReadOnlyDictionary<string, EquipoResponse> teams)
    {
        var tournamentTeams = seed.TeamKeys.Select(k => teams[k]).ToArray();
        var existing = await GetOrEmptyAsync<PartidaPorTorneoResponse>(http, $"/api/partidas/por-torneo/{torneo.TorneoId}");
        foreach (var match in SeedData.BuildMatches(seed, tournamentTeams))
        {
            if (existing.Any(p =>
                    Same(p.NombreLocal, match.Local.Nombre) &&
                    Same(p.NombreVisitante, match.Visitante.Nombre) &&
                    Same(p.Resultado, match.Resultado) &&
                    SameInstant(p.Fecha, match.Fecha)))
            {
                continue;
            }

            await PostAsync<PartidaResponse>(http, "/api/partidas", new
            {
                torneoId = torneo.TorneoId,
                nombreTorneo = torneo.Nombre,
                fecha = match.Fecha,
                equipoLocalId = match.Local.EquipoId,
                nombreLocal = match.Local.Nombre,
                equipoVisitanteId = match.Visitante.EquipoId,
                nombreVisitante = match.Visitante.Nombre,
                equipoGanadorId = match.Ganador.EquipoId,
                resultado = match.Resultado
            });
            Console.WriteLine($"  Partida creada: {torneo.Codigo} {match.Local.Tag} vs {match.Visitante.Tag}");
        }
    }

    private static async Task WaitForRankingAsync(HttpClient http)
    {
        for (var i = 1; i <= 30; i++)
        {
            var equipos = await GetOrEmptyAsync<RankingEquipoResponse>(http, "/api/ranking/equipos?top=10");
            var victorias = await GetOrEmptyAsync<RankingVictoriaResponse>(http, "/api/ranking/victorias?top=10");
            var jugadores = await GetOrEmptyAsync<RankingJugadorResponse>(http, "/api/ranking/jugadores?top=10");

            if (equipos.Any() && victorias.Any() && jugadores.Any())
            {
                Console.WriteLine("Ranking actualizado por eventos.");
                return;
            }

            Console.WriteLine($"Esperando eventos de ranking ({i}/30)...");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("Ranking no reflejo los eventos del seed dentro del tiempo esperado.");
    }

    private static async Task PrintSummaryAsync(HttpClient http)
    {
        var equipos = await GetOrEmptyAsync<EquipoResponse>(http, "/api/equipos/por-fecha");
        var torneos = await GetOrEmptyAsync<TorneoResumenResponse>(http, "/api/torneos/por-fecha");
        var organizadores = await GetOrEmptyAsync<OrganizadorResponse>(http, "/api/organizadores");
        var rankingEquipos = await GetOrEmptyAsync<RankingEquipoResponse>(http, "/api/ranking/equipos?top=50");
        var rankingVictorias = await GetOrEmptyAsync<RankingVictoriaResponse>(http, "/api/ranking/victorias?top=50");
        var rankingJugadores = await GetOrEmptyAsync<RankingJugadorResponse>(http, "/api/ranking/jugadores?top=50");

        Console.WriteLine();
        Console.WriteLine("Resumen del seed:");
        Console.WriteLine($"  - Equipos: {equipos.Count}");
        Console.WriteLine($"  - Torneos: {torneos.Count}");
        Console.WriteLine($"  - Organizadores: {organizadores.Count}");
        Console.WriteLine($"  - Ranking equipos por torneos: {rankingEquipos.Count}");
        Console.WriteLine($"  - Ranking equipos por victorias: {rankingVictorias.Count}");
        Console.WriteLine($"  - Ranking jugadores activos: {rankingJugadores.Count}");
    }

    private static async Task<List<T>> GetOrEmptyAsync<T>(HttpClient http, string path)
    {
        var result = await GetOptionalAsync<List<T>>(http, path);
        return result ?? [];
    }

    private static async Task<T> GetRequiredAsync<T>(HttpClient http, string path)
        where T : class
    {
        return await GetOptionalAsync<T>(http, path)
            ?? throw new InvalidOperationException($"GET {path} no devolvio datos.");
    }

    private static async Task<T?> GetOptionalAsync<T>(HttpClient http, string path)
        where T : class
    {
        using var response = await http.GetAsync(path);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GET {path} => {(int)response.StatusCode} {response.StatusCode}: {raw}");

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return JsonSerializer.Deserialize<T>(raw, JsonOptions);
    }

    private static async Task<T> PostAsync<T>(HttpClient http, string path, object body)
        where T : class
    {
        using var response = await http.PostAsync(path, ToJson(body));
        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"POST {path} => {(int)response.StatusCode} {response.StatusCode}: {raw}");

        return JsonSerializer.Deserialize<T>(raw, JsonOptions)
            ?? throw new InvalidOperationException($"POST {path} no devolvio JSON valido: {raw}");
    }

    private static async Task PostNoBodyAsync(HttpClient http, string path, object body)
    {
        using var response = await http.PostAsync(path, ToJson(body));
        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"POST {path} => {(int)response.StatusCode} {response.StatusCode}: {raw}");
    }

    private static StringContent ToJson(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            .ToUniversalTime();

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static bool Same(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool SameInstant(DateTimeOffset left, DateTimeOffset right) =>
        left.ToUniversalTime().ToUnixTimeSeconds() == right.ToUniversalTime().ToUnixTimeSeconds();
}

internal static class SeedData
{
    public static readonly GameSeed[] Games =
    [
        new("LOL", "League of Legends", "MOBA"),
        new("VAL", "Valorant", "FPS"),
        new("CS2", "Counter-Strike 2", "FPS"),
        new("DOTA2", "Dota 2", "MOBA"),
        new("RL", "Rocket League", "SPORTS")
    ];

    public static readonly OrganizerSeed[] Organizers =
    [
        new("RIOT", "Riot Games"),
        new("LOLE", "LoL Esports"),
        new("VCT", "VALORANT Champions Tour"),
        new("ESL", "ESL FACEIT Group"),
        new("BLAST", "BLAST Premier"),
        new("PGL", "PGL"),
        new("UNIVALLE", "UNIVALLE Esports")
    ];

    public static readonly TeamSeed[] Teams =
    [
        new("LOL_BLG", "Bilibili Gaming", "BLG", "CN", "LOL", ["CN"]),
        new("LOL_TES", "Top Esports", "TES", "CN", "LOL", ["CN"]),
        new("LOL_HLE", "Hanwha Life Esports", "HLE", "KR", "LOL", ["KR"]),
        new("LOL_GENG", "Gen.G", "GEN", "KR", "LOL", ["KR"],
            [
                new PlayerSeed("Kiin",   "Kim Gi-in",     "KR", "TOP"),
                new PlayerSeed("Canyon", "Kim Geon-bu",   "KR", "JUNGLE"),
                new PlayerSeed("Chovy",  "Jeong Ji-hoon", "KR", "MID"),
                new PlayerSeed("Ruler",  "Park Jae-hyuk", "KR", "ADC"),
                new PlayerSeed("Duro",   "Joo Min-kyu",   "KR", "SUPPORT")
            ]),
        new("LOL_T1", "T1", "T1", "KR", "LOL", ["KR"],
            [
                new PlayerSeed("Faker",    "Lee Sang-hyeok", "KR", "MID"),
                new PlayerSeed("Gumayusi", "Choi Yeon-ho",   "KR", "ADC"),
                new PlayerSeed("Zeus",     "Choi Woo-je",    "KR", "TOP")
            ]),
        new("LOL_G2", "G2 Esports", "G2", "DE", "LOL", ["DE", "ES", "DK", "PL"]),
        new("LOL_KC", "Karmine Corp", "KC", "FR", "LOL", ["FR", "BE"]),
        new("LOL_LYON", "LYON", "LYON", "US", "LOL", ["US", "CA"]),
        new("LOL_TLAW", "Team Liquid Alienware", "TLAW", "US", "LOL", ["US", "CA", "KR"]),
        new("LOL_TSW", "Team Secret Whales", "TSW", "VN", "LOL", ["VN"]),
        new("LOL_DCG", "Deep Cross Gaming", "DCG", "TW", "LOL", ["TW"]),
        new("LOL_FURIA", "FURIA", "FUR", "BR", "LOL", ["BR"]),
        new("LOL_FNC", "Fnatic", "FNC", "GB", "LOL", ["GB", "SE", "CZ", "DE"]),

        new("CS_VIT", "Team Vitality CS2", "VIT", "FR", "CS2", ["FR", "GB", "IL", "EE"]),
        new("CS_SPIRIT", "Team Spirit CS2", "TSPI", "EU", "CS2", ["RS", "UA", "RU"]),
        new("CS_MOUZ", "MOUZ CS2", "MOUZ", "DE", "CS2", ["DE", "SE", "HU", "FI"]),
        new("CS_NAVI", "Natus Vincere CS2", "NAVI", "UA", "CS2", ["UA", "RO", "LT"],
            [
                new PlayerSeed("s1mple",    "Oleksandr Kostyliev", "UA", "AWP"),
                new PlayerSeed("electronic", "Denis Sharipov",     "UA", "RIFLER")
            ]),
        new("CS_FAZE", "FaZe Clan CS2", "FAZE", "US", "CS2", ["US", "DK", "NO", "LV"],
            [
                new PlayerSeed("karrigan", "Finn Andersen",       "DK", "IGL"),
                new PlayerSeed("ropz",     "Robin Kool",          "EE", "RIFLER"),
                new PlayerSeed("broky",    "Helvijs Saukants",    "LV", "AWP"),
                new PlayerSeed("rain",     "Havard Nygaard",      "NO", "RIFLER"),
                new PlayerSeed("twistzz",  "Russel Van Dulken",   "CA", "RIFLER")
            ]),
        new("CS_TL", "Team Liquid CS2", "TLCS", "US", "CS2", ["US", "CA", "BR"]),
        new("CS_FURIA", "FURIA CS2", "FURCS", "BR", "CS2", ["BR"]),
        new("CS_MONG", "The MongolZ", "MONG", "MN", "CS2", ["MN"]),
        new("CS_PAIN", "paiN Gaming CS2", "PAIN", "BR", "CS2", ["BR"]),
        new("CS_FALC", "Team Falcons CS2", "FALC", "SA", "CS2", ["SA", "DK", "RU"]),
        new("CS_ASTR", "Astralis CS2", "ASTR", "DK", "CS2", ["DK"]),
        new("CS_G2", "G2 Esports CS2", "G2CS", "DE", "CS2", ["DE", "RU", "BA", "PL"]),

        new("VAL_G2", "G2 Esports Valorant", "G2V", "US", "VAL", ["US", "CA"]),
        new("VAL_SEN", "Sentinels", "SEN", "US", "VAL", ["US", "MA"]),
        new("VAL_MIBR", "MIBR Valorant", "MIBRV", "BR", "VAL", ["BR"]),
        new("VAL_NRG", "NRG Valorant", "NRG", "US", "VAL", ["US", "CA"]),
        new("VAL_PRX", "Paper Rex", "PRX", "SG", "VAL", ["SG", "ID", "MY"]),
        new("VAL_T1", "T1 Valorant", "T1V", "KR", "VAL", ["KR"]),
        new("VAL_RRQ", "Rex Regum Qeon", "RRQ", "ID", "VAL", ["ID"]),
        new("VAL_KRX", "Kiwoom DRX", "KRX", "KR", "VAL", ["KR"]),
        new("VAL_FNC", "Fnatic Valorant", "FNCV", "GB", "VAL", ["GB", "FI", "TR", "SE"]),
        new("VAL_TL", "Team Liquid Valorant", "TLV", "NL", "VAL", ["NL", "GB", "BE"]),
        new("VAL_GX", "GIANTX", "GX", "ES", "VAL", ["ES", "DE", "TR"]),
        new("VAL_TH", "Team Heretics", "TH", "ES", "VAL", ["ES", "LT", "TR"]),
        new("VAL_BLG", "Bilibili Gaming Valorant", "BLGV", "CN", "VAL", ["CN"]),
        new("VAL_EDG", "EDward Gaming", "EDG", "CN", "VAL", ["CN"]),
        new("VAL_XLG", "Xi Lai Gaming", "XLG", "CN", "VAL", ["CN"]),
        new("VAL_DRG", "Dragon Ranger Gaming", "DRG", "CN", "VAL", ["CN"])
    ];

    public static readonly TournamentSeed[] Tournaments =
    [
        new("LOL_MSI26", "League of Legends MSI 2026", "MSI26", "LOL", "LOLE", "2026-06-28T12:00:00Z", 350000m,
            ["LOL_BLG", "LOL_TES", "LOL_HLE", "LOL_T1", "LOL_G2", "LOL_KC", "LOL_LYON", "LOL_TLAW", "LOL_TSW", "LOL_DCG", "LOL_FURIA"]),
        new("LOL_WORLDS25", "League of Legends Worlds 2025", "WORLDS25", "LOL", "RIOT", "2025-10-14T08:00:00Z", 500000m,
            ["LOL_T1", "LOL_G2", "LOL_FNC", "LOL_BLG", "LOL_TES", "LOL_HLE", "LOL_FURIA", "LOL_TLAW", "LOL_TSW", "LOL_KC"]),
        new("LOL_LEC26", "LEC Summer 2026", "LEC-SUM26", "LOL", "LOLE", "2026-07-25T17:00:00Z", 120000m,
            ["LOL_G2", "LOL_KC", "LOL_FNC", "LOL_TLAW", "LOL_HLE", "LOL_BLG", "LOL_T1", "LOL_TES"]),
        new("LOL_RIFT_LIVE26", "Rift Live Showcase 2026", "RIFT-LIVE26", "LOL", "RIOT", "2026-06-17T20:00:00Z", 0m,
            ["LOL_T1", "LOL_GENG"], GeneratePrizePack: false, GenerateMatches: false),

        new("VAL_CHAMP25", "VALORANT Champions Paris 2025", "VCT-CHAMP25", "VAL", "VCT", "2025-09-12T15:00:00Z", 1000000m,
            ["VAL_G2", "VAL_SEN", "VAL_MIBR", "VAL_NRG", "VAL_PRX", "VAL_T1", "VAL_RRQ", "VAL_KRX", "VAL_FNC", "VAL_TL", "VAL_GX", "VAL_TH", "VAL_BLG", "VAL_EDG", "VAL_XLG", "VAL_DRG"]),
        new("VAL_AMER25", "VCT Americas Stage 2 2025", "VCT-AMER-S2-25", "VAL", "VCT", "2025-07-18T20:00:00Z", 250000m,
            ["VAL_G2", "VAL_SEN", "VAL_MIBR", "VAL_NRG", "VAL_PRX", "VAL_T1", "VAL_RRQ", "VAL_KRX"]),
        new("VAL_EMEA25", "VCT EMEA Stage 2 2025", "VCT-EMEA-S2-25", "VAL", "VCT", "2025-07-16T18:00:00Z", 250000m,
            ["VAL_FNC", "VAL_TL", "VAL_GX", "VAL_TH", "VAL_G2", "VAL_SEN", "VAL_EDG", "VAL_BLG"]),
        new("VAL_MASTERS25", "VALORANT Masters Toronto 2025", "MASTERS-TOR25", "VAL", "RIOT", "2025-06-07T17:00:00Z", 650000m,
            ["VAL_G2", "VAL_FNC", "VAL_PRX", "VAL_RRQ", "VAL_T1", "VAL_TL", "VAL_EDG", "VAL_NRG"]),

        new("CS_IEM26", "IEM Cologne Major 2026", "IEM-COL26", "CS2", "ESL", "2026-06-02T12:00:00Z", 500000m,
            ["CS_VIT", "CS_SPIRIT", "CS_MOUZ", "CS_NAVI", "CS_FAZE", "CS_TL", "CS_FURIA", "CS_MONG", "CS_PAIN", "CS_FALC", "CS_ASTR", "CS_G2"]),
        new("CS_BLAST_AUS25", "BLAST.tv Austin Major 2025", "BLAST-AUS25", "CS2", "BLAST", "2025-06-03T16:00:00Z", 500000m,
            ["CS_VIT", "CS_MONG", "CS_PAIN", "CS_FURIA", "CS_NAVI", "CS_FAZE", "CS_G2", "CS_TL", "CS_SPIRIT", "CS_MOUZ", "CS_FALC", "CS_ASTR"]),
        new("CS_PORTO26", "BLAST Premier Open Porto 2026", "BLAST-PORTO26", "CS2", "BLAST", "2026-08-26T15:00:00Z", 150000m,
            ["CS_VIT", "CS_FALC", "CS_G2", "CS_FURIA", "CS_NAVI", "CS_FAZE", "CS_ASTR", "CS_MOUZ"]),
        new("CS_FORTWORTH26", "BLAST Premier Rivals Fort Worth 2026", "BLAST-FW26", "CS2", "BLAST", "2026-05-01T18:00:00Z", 300000m,
            ["CS_VIT", "CS_ASTR", "CS_G2", "CS_FALC", "CS_FURIA", "CS_NAVI", "CS_FAZE", "CS_MOUZ"]),

        new("UNI_CUP26", "UNIVALLE Esports Invitational 2026", "UNI-INV26", "VAL", "UNIVALLE", "2026-09-10T18:30:00Z", 25000m,
            ["VAL_G2", "VAL_SEN", "VAL_FNC", "VAL_TL", "VAL_NRG", "VAL_PRX", "VAL_T1", "VAL_TH"])
    ];

    private static readonly string[] LoLRoles = ["TOP", "JUNGLE", "MID", "ADC", "SUPPORT"];
    private static readonly string[] ValorantRoles = ["DUELIST", "CONTROLLER", "INITIATOR", "SENTINEL", "FLEX"];
    private static readonly string[] CsRoles = ["IGL", "ENTRY", "AWP", "RIFLER", "LURKER"];
    private static readonly string[] DefaultRoles = ["CAPTAIN", "PLAYMAKER", "SUPPORT", "FLEX", "ANALYST"];
    private static readonly string[] Handles = ["Aegis", "Blaze", "Cipher", "Drift", "Echo", "Flux", "Ghost", "Hex", "Ion", "Jolt"];
    private static readonly string[] GivenNames = ["Alex", "Minjun", "Santiago", "Mateo", "Lucas", "Noah", "Kai", "Bruno", "Diego", "Elias", "Marek", "Leo"];
    private static readonly string[] Surnames = ["Kim", "Lee", "Garcia", "Silva", "Nielsen", "Novak", "Chen", "Park", "Smith", "Rossi", "Muller", "Kowalski"];

    public static IEnumerable<PlayerSeed> BuildRoster(TeamSeed team)
    {
        if (team.Players is { Length: > 0 })
            return team.Players;

        var roles = team.GameCode switch
        {
            "LOL" => LoLRoles,
            "VAL" => ValorantRoles,
            "CS2" => CsRoles,
            _ => DefaultRoles
        };

        var stem = new string(team.Tag.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var seed = team.Tag.Sum(c => c);
        return roles.Select((role, index) =>
        {
            var country = team.PlayerCountries[index % team.PlayerCountries.Length];
            var handle = Handles[(seed + index) % Handles.Length];
            var given = GivenNames[(seed + index * 2) % GivenNames.Length];
            var surname = Surnames[(seed + index * 3) % Surnames.Length];
            return new PlayerSeed($"{stem}{handle}", $"{given} {surname}", country, role);
        });
    }

    public static IEnumerable<GeneratedMatch> BuildMatches(TournamentSeed tournament, IReadOnlyList<EquipoResponse> teams)
    {
        var start = DateTimeOffset.Parse(tournament.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            .ToUniversalTime();
        var rounds = Math.Min(4, Math.Max(1, teams.Count - 1));
        var matchNumber = 0;

        for (var round = 0; round < rounds; round++)
        {
            for (var i = 0; i < teams.Count - 1; i += 2)
            {
                var local = teams[(i + round) % teams.Count];
                var visitor = teams[(i + round + 1) % teams.Count];
                if (local.EquipoId == visitor.EquipoId)
                    continue;

                var winner = (matchNumber + round) % 3 == 0 ? visitor : local;
                var fecha = start.AddDays(round + 1).AddHours(14 + (i / 2 * 2));
                yield return new GeneratedMatch(local, visitor, winner, ScoreFor(tournament.GameCode, matchNumber, false), fecha);
                matchNumber++;
            }
        }

        if (teams.Count >= 2)
        {
            var finalDate = start.AddDays(rounds + 3).AddHours(20);
            yield return new GeneratedMatch(teams[0], teams[1], teams[0], ScoreFor(tournament.GameCode, matchNumber, true), finalDate);
        }
    }

    private static string ScoreFor(string gameCode, int matchNumber, bool final)
    {
        if (gameCode == "CS2")
        {
            var scores = new[] { "13-8", "13-10", "16-14", "13-11", "19-17" };
            return scores[matchNumber % scores.Length];
        }

        if (final)
            return matchNumber % 2 == 0 ? "3-1" : "3-2";

        var series = new[] { "2-0", "2-1", "1-0", "2-1" };
        return series[matchNumber % series.Length];
    }
}

internal sealed record GameSeed(string Code, string Nombre, string Genero);
internal sealed record OrganizerSeed(string Code, string Nombre);
internal sealed record TeamSeed(string Key, string Nombre, string Tag, string Pais, string GameCode, string[] PlayerCountries, PlayerSeed[]? Players = null);
internal sealed record PlayerSeed(string Nickname, string Nombre, string Pais, string Rol);
internal sealed record TournamentSeed(
    string Key,
    string Nombre,
    string Codigo,
    string GameCode,
    string OrganizerCode,
    string FechaInicio,
    decimal BasePrize,
    string[] TeamKeys,
    bool GeneratePrizePack = true,
    bool GenerateMatches = true);
internal sealed record PrizeSeed(string Tipo, decimal Monto, Guid EquipoId);
internal sealed record GeneratedMatch(EquipoResponse Local, EquipoResponse Visitante, EquipoResponse Ganador, string Resultado, DateTimeOffset Fecha);

internal sealed record LoginResponse(string Token, string Rol, string Nombre, Guid? OrganizadorId, Guid? EquipoId, DateTimeOffset ExpiraEn);
internal sealed record VideojuegoResponse(Guid VideojuegoId, string Nombre, string Genero);
internal sealed record VideojuegoPorGeneroResponse(Guid VideojuegoId, string Nombre);
internal sealed record OrganizadorResponse(Guid OrganizadorId, string Nombre);
internal sealed record EquipoResponse(Guid EquipoId, string Nombre, string Tag, string Pais, DateTimeOffset FechaCreacion);
internal sealed record JugadorResponse(Guid JugadorId, string Codigo, string Nickname, string Nombre, string Pais, string Rol, Guid? EquipoId);
internal sealed record MembresiaResponse(Guid EquipoId, string NombreEquipo, string Tag, string Rol, DateTimeOffset FechaDesde, DateTimeOffset? FechaHasta, bool Activa);
internal sealed record TorneoResponse(Guid TorneoId, string Nombre, string Codigo, Guid VideojuegoId, string NombreVideojuego, Guid OrganizadorId, string NombreOrganizador, DateTimeOffset FechaInicio);
internal sealed record TorneoPorCodigoResponse(Guid TorneoId, string Nombre, DateTimeOffset FechaInicio);
internal sealed record TorneoResumenResponse(Guid TorneoId, string NombreTorneo, string NombreVideojuego, DateTimeOffset FechaInicio);
internal sealed record EquipoPorTorneoResponse(Guid EquipoId, string NombreEquipo, DateTimeOffset FechaInscripcion);
internal sealed record PremioResponse(Guid PremioId, Guid TorneoId, decimal Monto, string Tipo, Guid? EquipoId, string? NombreEquipo);
internal sealed record PartidaResponse(Guid PartidaId, Guid TorneoId, string NombreTorneo, DateTimeOffset Fecha, Guid EquipoLocalId, Guid EquipoVisitanteId, string NombreLocal, string NombreVisitante, Guid EquipoGanadorId, string Resultado);
internal sealed record PartidaPorTorneoResponse(Guid PartidaId, string NombreLocal, string NombreVisitante, string Resultado, DateTimeOffset Fecha);
internal sealed record RankingEquipoResponse(int Posicion, Guid EquipoId, long TotalTorneos);
internal sealed record RankingVictoriaResponse(int Posicion, Guid EquipoId, long TotalVictorias);
internal sealed record RankingJugadorResponse(int Posicion, Guid JugadorId, long TotalTorneos);
