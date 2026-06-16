using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

[Collection("Gateway")]
public class TournamentsTests(GatewayFixture fix, ITestOutputHelper output)
{
    // ─── Q8: Videojuegos por género ─────────────────────────────────────────────

    [Fact]
    public async Task Q8_GeneroMOBA_Devuelve200_Con2Juegos()
    {
        var r = await fix.Http.GetAsync("/api/videojuegos/por-genero/MOBA");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, arr.GetArrayLength());
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombre").GetString()).ToList();
        Assert.Contains("League of Legends", nombres);
        Assert.Contains("Dota 2",            nombres);
    }

    [Fact]
    public async Task Q8_GeneroFPS_Devuelve200_Con2Juegos()
    {
        var r = await fix.Http.GetAsync("/api/videojuegos/por-genero/FPS");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, arr.GetArrayLength());
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombre").GetString()).ToList();
        Assert.Contains("Valorant",         nombres);
        Assert.Contains("Counter-Strike 2", nombres);
    }

    [Fact]
    public async Task Q8_GeneroInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync("/api/videojuegos/por-genero/RTS");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q9: Torneos por videojuego ─────────────────────────────────────────────

    [Fact]
    public async Task Q9_LoL_Devuelve200_ConAlMenos2Torneos()
    {
        var r = await fix.Http.GetAsync($"/api/videojuegos/{fix.LoLId}/torneos");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2, $"Se esperaban >= 2 torneos de LoL, hay {arr.GetArrayLength()}");

        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("torneoId", out _),          "Falta torneoId");
            Assert.True(el.TryGetProperty("nombreTorneo", out _),      "Falta nombreTorneo");
            Assert.True(el.TryGetProperty("nombreOrganizador", out _), "Falta nombreOrganizador");
            Assert.True(el.TryGetProperty("fechaInicio", out _),       "Falta fechaInicio");
        }

        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.True(nombres.Any(n => n != null && n.Contains("Worlds 2025")), "Falta Worlds 2025 en LoL");
        Assert.True(nombres.Any(n => n != null && n.Contains("MSI 2026")),    "Falta MSI 2026 en LoL");
    }

    [Fact]
    public async Task Q9_Valorant_Devuelve200_ConAlMenos3Torneos()
    {
        var r = await fix.Http.GetAsync($"/api/videojuegos/{fix.ValId}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 3, $"Se esperaban >= 3 torneos de VAL, hay {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q9_VideojuegoIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/videojuegos/{Guid.NewGuid()}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q10: Organizadores ─────────────────────────────────────────────────────

    [Fact]
    public async Task Q10_Organizadores_Devuelve200_Con3Organizadores()
    {
        var r = await fix.Http.GetAsync("/api/organizadores");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 3, $"Se esperaban >= 3 organizadores, hay {arr.GetArrayLength()}");
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombre").GetString()).ToList();
        Assert.Contains("ESL FACEIT Group", nombres);
        Assert.Contains("PGL",              nombres);
        Assert.Contains("BLAST Premier",    nombres);
    }

    // ─── Q11: Torneos por organizador ───────────────────────────────────────────

    [Fact]
    public async Task Q11_ESL_Devuelve200_ConIEM()
    {
        var r = await fix.Http.GetAsync($"/api/organizadores/{fix.ESLId}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1);
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.True(nombres.Any(n => n != null && n.Contains("IEM")), "ESL debe organizar IEM Cologne");
    }

    [Fact]
    public async Task Q11_BLAST_Devuelve200_ConAlMenos3Torneos()
    {
        var r = await fix.Http.GetAsync($"/api/organizadores/{fix.BLASTOrgId}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 3, $"BLAST debe tener >= 3 torneos, tiene {arr.GetArrayLength()}");
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.True(nombres.Any(n => n != null && n.Contains("BLAST")));
    }

    [Fact]
    public async Task Q11_OrganizadorIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/organizadores/{Guid.NewGuid()}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q12: Torneos por fecha (más reciente primero) ──────────────────────────

    [Fact]
    public async Task Q12_PorFecha_Devuelve200_Con5Torneos_OrdenDescendente()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-fecha");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 5, $"Se esperaban >= 5 torneos, hay {arr.GetArrayLength()}");

        var fechas = arr.EnumerateArray()
            .Select(e => DateTimeOffset.Parse(e.GetProperty("fechaInicio").GetString()!))
            .ToList();
        for (int i = 1; i < fechas.Count; i++)
            Assert.True(fechas[i - 1] >= fechas[i],
                $"Torneos fuera de orden descendente: {fechas[i-1]} < {fechas[i]}");
    }

    [Fact]
    public async Task Q12_CadaTorneo_TieneNombreVideojuego()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-fecha");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        foreach (var el in arr.EnumerateArray())
            Assert.True(el.TryGetProperty("nombreVideojuego", out _), "Falta nombreVideojuego en Q12");
    }

    // ─── Q13: Equipos por torneo ────────────────────────────────────────────────

    [Fact]
    public async Task Q13_Worlds_Devuelve200_ConT1_G2_FNC()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.WorldsId}/equipos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 8, $"WORLDS25 debería tener >= 8 equipos, hay {arr.GetArrayLength()}");
        var equipos = arr.EnumerateArray().Select(e => e.GetProperty("nombreEquipo").GetString()).ToList();
        Assert.Contains("T1",      equipos);
        Assert.Contains("G2 Esports", equipos);
        Assert.Contains("Fnatic",  equipos);
    }

    [Fact]
    public async Task Q13_IEM_Devuelve200_ConNAVI_FAZE()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.IEMId}/equipos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 8, $"IEM-COL26 debería tener >= 8 equipos, hay {arr.GetArrayLength()}");
        var equipos = arr.EnumerateArray().Select(e => e.GetProperty("nombreEquipo").GetString()).ToList();
        Assert.Contains("Natus Vincere CS2", equipos);
        Assert.Contains("FaZe Clan CS2",     equipos);
    }

    [Fact]
    public async Task Q13_TorneoIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{Guid.NewGuid()}/equipos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q14: Torneos por equipo ────────────────────────────────────────────────

    [Fact]
    public async Task Q14_T1_Devuelve200_ConWorldsYMSI()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/por-equipo/{fix.T1Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2);
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.True(nombres.Any(n => n != null && n.Contains("Worlds 2025")), "T1 debe estar en Worlds 2025");
        Assert.True(nombres.Any(n => n != null && n.Contains("MSI 2026")),    "T1 debe estar en MSI 2026");
    }

    [Fact]
    public async Task Q14_NAVI_Devuelve200_ConAlMenos1Torneo()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/por-equipo/{fix.NAVIId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1, "NAVI debería estar en al menos IEM-COL26");
    }

    [Fact]
    public async Task Q14_EquipoIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/por-equipo/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q15: Torneo por código único ───────────────────────────────────────────

    [Fact]
    public async Task Q15_CodigoWORLDS25_Devuelve200_ConNombreYFecha()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-codigo/WORLDS25");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(doc.GetProperty("nombre").GetString()!.Contains("Worlds 2025"));
        Assert.NotEqual(Guid.Empty, doc.GetProperty("torneoId").GetGuid());
        Assert.True(doc.TryGetProperty("fechaInicio", out _));
    }

    [Fact]
    public async Task Q15_CodigoIEM_COL26_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-codigo/IEM-COL26");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(doc.GetProperty("nombre").GetString()!.Contains("IEM"));
    }

    [Fact]
    public async Task Q15_CodigoInexistente_Devuelve404()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-codigo/TORNEO_QUE_NO_EXISTE");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ─── Q20: Premios por torneo ────────────────────────────────────────────────

    [Fact]
    public async Task Q20_Worlds_Devuelve200_Con4Premios()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.WorldsId}/premios");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(4, arr.GetArrayLength());

        var montos = arr.EnumerateArray()
            .Select(e => e.GetProperty("monto").GetDecimal())
            .OrderByDescending(m => m)
            .ToList();
        Assert.Equal(500000m, montos[0]);   // Campeon
        Assert.Equal(225000m, montos[1]);   // Subcampeon 45%
        Assert.Equal(100000m, montos[2]);   // Semifinalista 20%
        Assert.Equal(40000m,  montos[3]);   // MVP 8%
    }

    [Fact]
    public async Task Q20_Worlds_Campeon_EsT1()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.WorldsId}/premios");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var campeon = arr.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("tipo").GetString() == "Campeon");
        Assert.NotEqual(default, campeon);
        Assert.Equal(fix.T1Id, campeon.GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task Q20_IEM_Devuelve200_Con4Premios()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.IEMId}/premios");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(4, arr.GetArrayLength());
    }

    // ─── Q21: Premios por equipo ────────────────────────────────────────────────

    [Fact]
    public async Task Q21_T1_Devuelve200_Con500kDeCampeon()
    {
        var r = await fix.Http.GetAsync($"/api/premios/por-equipo/{fix.T1Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1);
        var montos = arr.EnumerateArray().Select(e => e.GetProperty("monto").GetDecimal()).ToList();
        Assert.Contains(500000m, montos);   // Campeon de WORLDS25
    }

    [Fact]
    public async Task Q21_NAVI_Devuelve200_Con1Premio_MVP_IEM()
    {
        var r = await fix.Http.GetAsync($"/api/premios/por-equipo/{fix.NAVIId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal(40000m, arr.EnumerateArray().First().GetProperty("monto").GetDecimal());
    }

    [Fact]
    public async Task Q21_FuriaLoL_SinPremios_Devuelve200_ListaVacia()
    {
        // FURIA LoL (FUR) está en posición 6+ en WORLDS25 y MSI26: no alcanza el top-4
        var r = await fix.Http.GetAsync($"/api/premios/por-equipo/{fix.FurId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }
}
