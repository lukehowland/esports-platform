using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Esports.Gateway.Tests;

[Collection("Gateway")]
public class AuthTests(GatewayFixture fix)
{
    [Fact]
    public async Task Login_Admin_DevuelveTokenYMeConUsername()
    {
        var token = await fix.LoginAsync("admin", "admin-dev-password");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await fix.Http.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = GatewayFixture.ParseJson(await response.Content.ReadAsStringAsync());
        Assert.Equal("admin", doc.GetProperty("username").GetString());
        Assert.Equal("admin", doc.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Login_DemoUsers_DevuelveRolesEsperados()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");
        var capToken = await fix.LoginAsync("cap_t1", "CapDemo2024");
        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");

        var riot = await GetMeAsync(riotToken);
        var cap = await GetMeAsync(capToken);
        var fan = await GetMeAsync(fanToken);

        Assert.Equal("organizador", riot.GetProperty("rol").GetString());
        Assert.Equal(fix.RIOTId, riot.GetProperty("organizadorId").GetGuid());
        Assert.Equal("capitan", cap.GetProperty("rol").GetString());
        Assert.Equal(fix.T1Id, cap.GetProperty("equipoId").GetGuid());
        Assert.Equal("fan", fan.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task GetPublico_RankingSinToken_Devuelve200()
    {
        var response = await fix.Http.GetAsync("/api/ranking/equipos?top=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MutacionSinToken_Devuelve401()
    {
        var response = await fix.Http.PostAsJsonAsync("/api/equipos", new
        {
            nombre = "Unauthorized Team",
            tag = $"UA{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            pais = "CO"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterSinToken_Devuelve401()
    {
        var response = await fix.Http.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"fan_noauth_{Guid.NewGuid():N}"[..24],
            password = "FanDemo2024",
            rol = "fan",
            nombreDisplay = "Fan No Auth"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FanNoPuedeRegistrarUsuario_Devuelve403()
    {
        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");

        var response = await fix.AuthedPost("/api/auth/register", new
        {
            username = $"fan_forbid_{Guid.NewGuid():N}"[..24],
            password = "FanDemo2024",
            rol = "fan",
            nombreDisplay = "Fan Forbidden"
        }, fanToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminPuedeRegistrarFan_Devuelve201()
    {
        using var response = await fix.AdminPost("/api/auth/register", new
        {
            username = $"fan_auth_{Guid.NewGuid():N}"[..24],
            password = "FanDemo2024",
            rol = "fan",
            nombreDisplay = "Fan Auth Test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AdminNoPuedeRegistrarRolInvalido_Devuelve400()
    {
        using var response = await fix.AdminPost("/api/auth/register", new
        {
            username = $"bad_role_{Guid.NewGuid():N}"[..24],
            password = "FanDemo2024",
            rol = "owner",
            nombreDisplay = "Bad Role"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminNoPuedeRegistrarOrganizadorSinOrganizadorId_Devuelve400()
    {
        using var response = await fix.AdminPost("/api/auth/register", new
        {
            username = $"org_bad_{Guid.NewGuid():N}"[..24],
            password = "OrgDemo2024",
            rol = "organizador",
            nombreDisplay = "Organizer Without Id"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminNoPuedeRegistrarCapitanSinEquipoId_Devuelve400()
    {
        using var response = await fix.AdminPost("/api/auth/register", new
        {
            username = $"cap_bad_{Guid.NewGuid():N}"[..24],
            password = "CapDemo2024",
            rol = "capitan",
            nombreDisplay = "Captain Without Team"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminNoPuedeRegistrarFanConVinculo_Devuelve400()
    {
        using var response = await fix.AdminPost("/api/auth/register", new
        {
            username = $"fan_bad_{Guid.NewGuid():N}"[..24],
            password = "FanDemo2024",
            rol = "fan",
            equipoId = fix.T1Id,
            nombreDisplay = "Fan With Team"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FanNoPuedeCrearVideojuego_Devuelve403()
    {
        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");

        var response = await fix.AuthedPost("/api/videojuegos", new
        {
            nombre = $"Fan Game {Guid.NewGuid():N}"[..18],
            genero = "FPS",
            plataforma = "PC"
        }, fanToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeCrearVideojuego_Devuelve403()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");

        var response = await fix.AuthedPost("/api/videojuegos", new
        {
            nombre = $"Auth Test Game {Guid.NewGuid():N}"[..28],
            genero = "AUTH",
            plataforma = "PC"
        }, riotToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeCrearOrganizador_Devuelve403()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");

        var response = await fix.AuthedPost("/api/organizadores", new
        {
            nombre = $"Auth Org {Guid.NewGuid():N}"[..20],
            email = "forbidden@test.gg"
        }, riotToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeCrearTorneoDeOtroOrganizador_Devuelve403()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");

        var response = await fix.AuthedPost("/api/torneos", new
        {
            nombre = "Riot Cannot Create ESL Tournament",
            codigo = $"AUTH-ESL-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            videojuegoId = fix.LoLId,
            organizadorId = fix.ESLId,
            fechaInicio = "2026-12-01T00:00:00Z",
            fechaFin = "2026-12-08T00:00:00Z"
        }, riotToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorPuedeCrearTorneoPropio_Devuelve201()
    {
        var organizer = await CreateTestOrganizerAsync();

        using var response = await fix.AuthedPost("/api/torneos", new
        {
            nombre = "Auth Smoke Tournament",
            codigo = $"AUTH-OWN-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            videojuegoId = fix.LoLId,
            organizadorId = organizer.OrganizadorId,
            fechaInicio = "2026-12-02T00:00:00Z",
            fechaFin = "2026-12-09T00:00:00Z"
        }, organizer.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CapitanNoPuedeInscribirEquipoAjeno_Devuelve403()
    {
        var capToken = await fix.LoginAsync("cap_t1", "CapDemo2024");

        var response = await fix.AuthedPost($"/api/torneos/{fix.WorldsId}/inscripciones", new
        {
            equipoId = fix.G2Id
        }, capToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CapitanPuedeAgregarJugadorASuEquipo_Devuelve201()
    {
        var team = await CreateTeamAsync();
        var captainToken = await CreateCaptainForTeamAsync(team.EquipoId);

        var response = await fix.AuthedPost($"/api/equipos/{team.EquipoId}/jugadores", new
        {
            nickname = $"CAP{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            nombre = "Captain Owned Player",
            pais = "CO",
            rol = "FLEX",
            email = "captain-owned@test.gg",
            telefono = "+1-555-1200"
        }, captainToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CapitanNoPuedeAgregarJugadorAEquipoAjeno_Devuelve403()
    {
        var capToken = await fix.LoginAsync("cap_t1", "CapDemo2024");

        var response = await fix.AuthedPost($"/api/equipos/{fix.G2Id}/jugadores", new
        {
            nickname = $"BAD{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            nombre = "Forbidden Player",
            pais = "CO",
            rol = "FLEX",
            email = "forbidden-player@test.gg",
            telefono = "+1-555-1201"
        }, capToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CapitanPuedeEditarJugadorPropio_PeroNoAjeno()
    {
        var team = await CreateTeamAsync();
        var captainToken = await CreateCaptainForTeamAsync(team.EquipoId);
        var nickname = $"EDIT{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        using var crear = await fix.AuthedPost($"/api/equipos/{team.EquipoId}/jugadores", new
        {
            nickname,
            nombre = "Player Before Edit",
            pais = "CO",
            rol = "FLEX",
            email = "before@test.gg",
            telefono = "+1-555-1300"
        }, captainToken);
        crear.EnsureSuccessStatusCode();
        var jugadorId = GatewayFixture.ParseJson(await crear.Content.ReadAsStringAsync())
            .GetProperty("jugadorId").GetGuid();

        using var propio = await fix.AuthedPut($"/api/jugadores/{jugadorId}", new
        {
            nombre = "Player After Edit",
            email = "after@test.gg",
            telefono = "+1-555-1301"
        }, captainToken);
        Assert.Equal(HttpStatusCode.OK, propio.StatusCode);

        using var rosterAjeno = await fix.Http.GetAsync($"/api/equipos/{fix.G2Id}/integrantes");
        var jugadorAjenoId = GatewayFixture.ParseJson(await rosterAjeno.Content.ReadAsStringAsync())
            .EnumerateArray().First().GetProperty("jugadorId").GetGuid();
        using var ajeno = await fix.AuthedPut($"/api/jugadores/{jugadorAjenoId}", new
        {
            nombre = "Forbidden Edit",
            email = "forbidden@test.gg",
            telefono = "+1-555-1302"
        }, captainToken);
        Assert.Equal(HttpStatusCode.Forbidden, ajeno.StatusCode);
    }

    [Fact]
    public async Task CapitanNoPuedeCrearEquipo_Devuelve403()
    {
        var capToken = await fix.LoginAsync("cap_t1", "CapDemo2024");

        var response = await fix.AuthedPost("/api/equipos", new
        {
            nombre = "Captain Forbidden Team",
            tag = $"CF{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            pais = "CO"
        }, capToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CapitanPuedeInscribirSuEquipo_Devuelve201()
    {
        var (tournamentId, _) = await CreateOwnedTournamentAsync();
        var capToken = await fix.LoginAsync("cap_t1", "CapDemo2024");

        var response = await fix.AuthedPost($"/api/torneos/{tournamentId}/inscripciones", new
        {
            equipoId = fix.T1Id
        }, capToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorPuedeAsignarPremioEnTorneoPropio_Devuelve201()
    {
        var (tournamentId, organizerToken) = await CreateOwnedTournamentAsync();
        await EnrollTeamAsync(tournamentId, fix.T1Id);

        var response = await fix.AuthedPost($"/api/torneos/{tournamentId}/premios", new
        {
            monto = 12345.67m,
            tipo = $"Auth Prize {Guid.NewGuid():N}"[..24],
            equipoId = fix.T1Id
        }, organizerToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeAsignarPremioAEquipoNoInscrito_Devuelve409()
    {
        var (tournamentId, organizerToken) = await CreateOwnedTournamentAsync();

        var response = await fix.AuthedPost($"/api/torneos/{tournamentId}/premios", new
        {
            monto = 12345.67m,
            tipo = $"Auth Bad Prize {Guid.NewGuid():N}"[..28],
            equipoId = fix.T1Id
        }, organizerToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeAsignarPremioEnTorneoAjeno_Devuelve403()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");

        var response = await fix.AuthedPost($"/api/torneos/{fix.IEMId}/premios", new
        {
            monto = 1000m,
            tipo = "Forbidden Prize",
            equipoId = fix.T1Id
        }, riotToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorPuedeRegistrarPartidaDeTorneoPropio_Devuelve201()
    {
        var (tournamentId, organizerToken) = await CreateOwnedTournamentAsync();
        await EnrollTeamAsync(tournamentId, fix.T1Id);
        await EnrollTeamAsync(tournamentId, fix.G2Id);

        var response = await fix.AuthedPost("/api/partidas", new
        {
            torneoId = tournamentId,
            nombreTorneo = "Auth Match Tournament",
            fecha = "2026-12-04T20:00:00Z",
            equipoLocalId = fix.T1Id,
            nombreLocal = "T1",
            equipoVisitanteId = fix.G2Id,
            nombreVisitante = "G2 Esports",
            equipoGanadorId = fix.T1Id,
            resultado = "2-1"
        }, organizerToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeRegistrarPartidaSinEquiposInscritos_Devuelve409()
    {
        var (tournamentId, organizerToken) = await CreateOwnedTournamentAsync();

        var response = await fix.AuthedPost("/api/partidas", new
        {
            torneoId = tournamentId,
            nombreTorneo = "Auth Match Tournament",
            fecha = "2026-12-04T20:00:00Z",
            equipoLocalId = fix.T1Id,
            nombreLocal = "T1",
            equipoVisitanteId = fix.G2Id,
            nombreVisitante = "G2 Esports",
            equipoGanadorId = fix.T1Id,
            resultado = "2-1"
        }, organizerToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task OrganizadorNoPuedeRegistrarPartidaDeTorneoAjeno_Devuelve403()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");

        var response = await fix.AuthedPost("/api/partidas", new
        {
            torneoId = fix.IEMId,
            nombreTorneo = "IEM Cologne Major 2026",
            fecha = "2026-06-20T12:00:00Z",
            equipoLocalId = fix.NAVIId,
            nombreLocal = "Natus Vincere CS2",
            equipoVisitanteId = fix.FAZEId,
            nombreVisitante = "FaZe Clan CS2",
            equipoGanadorId = fix.NAVIId,
            resultado = "13-11"
        }, riotToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<JsonElement> GetMeAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await fix.Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return GatewayFixture.ParseJson(await response.Content.ReadAsStringAsync());
    }

    private async Task<(Guid EquipoId, string Tag)> CreateTeamAsync()
    {
        var tag = $"AT{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        using var response = await fix.AdminPost("/api/equipos", new
        {
            nombre = $"Auth Team {tag}",
            tag,
            pais = "CO"
        });
        response.EnsureSuccessStatusCode();
        var doc = GatewayFixture.ParseJson(await response.Content.ReadAsStringAsync());
        return (doc.GetProperty("equipoId").GetGuid(), tag);
    }

    private async Task<string> CreateCaptainForTeamAsync(Guid equipoId)
    {
        var username = $"cap_auth_{Guid.NewGuid():N}"[..24].ToLowerInvariant();
        using var response = await fix.AdminPost("/api/auth/register", new
        {
            username,
            password = "CapDemo2024",
            rol = "capitan",
            equipoId,
            nombreDisplay = "Capitan Auth Test"
        });
        response.EnsureSuccessStatusCode();
        return await fix.LoginAsync(username, "CapDemo2024");
    }

    private async Task<(Guid TorneoId, string OrganizadorToken)> CreateOwnedTournamentAsync()
    {
        var organizer = await CreateTestOrganizerAsync();
        using var response = await fix.AuthedPost("/api/torneos", new
        {
            nombre = "Auth Ownership Tournament",
            codigo = $"AUTH-T{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            videojuegoId = fix.LoLId,
            organizadorId = organizer.OrganizadorId,
            fechaInicio = "2026-12-03T00:00:00Z",
            fechaFin = "2026-12-10T00:00:00Z"
        }, organizer.Token);
        response.EnsureSuccessStatusCode();
        var doc = GatewayFixture.ParseJson(await response.Content.ReadAsStringAsync());
        return (doc.GetProperty("torneoId").GetGuid(), organizer.Token);
    }

    private async Task<(Guid OrganizadorId, string Token)> CreateTestOrganizerAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var organizerResponse = await fix.AdminPost("/api/organizadores", new
        {
            nombre = $"Auth Test Organizer {suffix}",
            email = $"auth-{suffix.ToLowerInvariant()}@test.gg"
        });
        organizerResponse.EnsureSuccessStatusCode();
        var organizerDoc = GatewayFixture.ParseJson(await organizerResponse.Content.ReadAsStringAsync());
        var organizerId = organizerDoc.GetProperty("organizadorId").GetGuid();

        var username = $"org_auth_{Guid.NewGuid():N}"[..24].ToLowerInvariant();
        using var userResponse = await fix.AdminPost("/api/auth/register", new
        {
            username,
            password = "OrgDemo2024",
            rol = "organizador",
            organizadorId = organizerId,
            nombreDisplay = "Auth Test Organizer"
        });
        userResponse.EnsureSuccessStatusCode();

        return (organizerId, await fix.LoginAsync(username, "OrgDemo2024"));
    }

    private async Task EnrollTeamAsync(Guid tournamentId, Guid teamId)
    {
        using var response = await fix.AdminPost($"/api/torneos/{tournamentId}/inscripciones", new
        {
            equipoId = teamId
        });
        response.EnsureSuccessStatusCode();
    }
}
