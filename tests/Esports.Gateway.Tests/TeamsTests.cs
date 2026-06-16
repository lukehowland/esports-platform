using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

/// <summary>
/// Tests de integración para el servicio teams (Q1–Q6) vía gateway :8080.
/// </summary>
[Collection("Gateway")]
public class TeamsTests(GatewayFixture fix, ITestOutputHelper output)
{
    // ─── Q1: Jugador por nickname ───────────────────────────────────────────────

    [Fact]
    public async Task Q1_Faker_Devuelve200_ConPropiedadesCorrectas()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-nickname/Faker");
        output.WriteLine(await r.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal("Faker", doc.GetProperty("nickname").GetString());
        Assert.Equal("KR",    doc.GetProperty("pais").GetString());
        Assert.Equal("MID",   doc.GetProperty("rol").GetString());
        Assert.NotEqual(Guid.Empty, doc.GetProperty("jugadorId").GetGuid());
    }

    [Fact]
    public async Task Q1_NicknameInexistente_Devuelve404()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-nickname/JugadorQueNoExiste999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Q1_s1mple_NaVi_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-nickname/s1mple");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal("UA", doc.GetProperty("pais").GetString());
        Assert.Equal("SNIPER", doc.GetProperty("rol").GetString());
    }

    // ─── Q2: Jugadores por país ─────────────────────────────────────────────────

    [Fact]
    public async Task Q2_PaisKR_Devuelve200_ConAlMenos5Jugadores()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-pais/KR");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 5, $"Se esperaban >= 5 jugadores KR, pero hay {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q2_PaisDK_Devuelve200_ConAlMenos1Jugador()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-pais/DK");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1, "karrigan (DK) debe aparecer");
        var nicknames = arr.EnumerateArray().Select(e => e.GetProperty("nickname").GetString()).ToList();
        Assert.Contains("karrigan", nicknames);
    }

    [Fact]
    public async Task Q2_PaisInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-pais/ZZ");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q3: Jugadores de un equipo (con/sin filtro de país) ───────────────────

    [Fact]
    public async Task Q3_T1_SinFiltro_Devuelve200_Con3Jugadores()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{fix.T1Id}/jugadores");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(3, arr.GetArrayLength());
        var nicknames = arr.EnumerateArray().Select(e => e.GetProperty("nickname").GetString()).ToList();
        Assert.Contains("Faker",    nicknames);
        Assert.Contains("Gumayusi", nicknames);
        Assert.Contains("Zeus",     nicknames);
    }

    [Fact]
    public async Task Q3_T1_FiltroKR_Devuelve200_SoloJugadoresKR()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{fix.T1Id}/jugadores?pais=KR");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1);
        foreach (var el in arr.EnumerateArray())
            Assert.Equal("KR", el.GetProperty("pais").GetString());
    }

    [Fact]
    public async Task Q3_T1_FiltroPaisInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{fix.T1Id}/jugadores?pais=ZZ");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q3_EquipoIdInexistente_Devuelve200_ListaVacia()
    {
        var fakeId = Guid.NewGuid();
        var r = await fix.Http.GetAsync($"/api/equipos/{fakeId}/jugadores");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q4: Equipos por fecha ──────────────────────────────────────────────────

    [Fact]
    public async Task Q4_PorFecha_Devuelve200_Con8Equipos()
    {
        var r = await fix.Http.GetAsync("/api/equipos/por-fecha");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 8, $"Se esperaban >= 8 equipos, hay {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q4_PorFecha_CadaElementoTienePropiedadesRequeridas()
    {
        var r = await fix.Http.GetAsync("/api/equipos/por-fecha");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("equipoId", out _),      "Falta equipoId");
            Assert.True(el.TryGetProperty("nombre", out _),        "Falta nombre");
            Assert.True(el.TryGetProperty("tag", out _),           "Falta tag");
            Assert.True(el.TryGetProperty("fechaCreacion", out _), "Falta fechaCreacion");
        }
    }

    // ─── Q5: Equipo por tag ─────────────────────────────────────────────────────

    [Fact]
    public async Task Q5_TagT1_Devuelve200_ConNombreYTagCorrectos()
    {
        var r = await fix.Http.GetAsync("/api/equipos/por-tag/T1");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal("T1", doc.GetProperty("nombre").GetString());
        Assert.Equal("T1", doc.GetProperty("tag").GetString());
        Assert.Equal("KR", doc.GetProperty("pais").GetString());
    }

    [Fact]
    public async Task Q5_TagDRX_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/api/equipos/por-tag/DRX");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal("DRX", doc.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Q5_TagInexistente_Devuelve404()
    {
        var r = await fix.Http.GetAsync("/api/equipos/por-tag/EQUIPO_QUE_NO_EXISTE");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ─── Q6: Integrantes del equipo ─────────────────────────────────────────────

    [Fact]
    public async Task Q6_IntegrantesT1_Devuelve200_ConJugadoresYRoles()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{fix.T1Id}/integrantes");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(3, arr.GetArrayLength());
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("jugadorId", out _), "Falta jugadorId");
            Assert.True(el.TryGetProperty("nickname", out _),  "Falta nickname");
            Assert.True(el.TryGetProperty("rol", out _),       "Falta rol");
        }
    }

    [Fact]
    public async Task Q6_IntegrantesNaVi_Devuelve200_Con2Jugadores()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{fix.NaViId}/integrantes");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, arr.GetArrayLength());
        var nicknames = arr.EnumerateArray().Select(e => e.GetProperty("nickname").GetString()).ToList();
        Assert.Contains("s1mple",    nicknames);
        Assert.Contains("electronic", nicknames);
    }

    [Fact]
    public async Task Q6_EquipoIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{Guid.NewGuid()}/integrantes");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Escritura: crear equipo y jugador ──────────────────────────────────────

    [Fact]
    public async Task POST_CrearEquipo_Devuelve201_ConEquipoId()
    {
        var uniqueTag = $"TST{Guid.NewGuid():N}"[..8].ToUpper();
        var body = JsonSerializer.Serialize(new { nombre = "Test Team", tag = uniqueTag, pais = "CO" });
        var r = await fix.Http.PostAsync("/api/equipos",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.NotEqual(Guid.Empty, doc.GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task POST_CrearEquipoConCuerpoVacio_Devuelve400()
    {
        var r = await fix.Http.PostAsync("/api/equipos",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.True(
            r.StatusCode == HttpStatusCode.BadRequest ||
            r.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Se esperaba 400/422 pero fue {r.StatusCode}");
    }
}
