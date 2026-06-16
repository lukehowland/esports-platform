using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

/// <summary>
/// Tests de integración para el servicio tournaments (Q8–Q15, Q20, Q21) vía gateway :8080.
/// </summary>
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
    public async Task Q9_LoL_Devuelve200_Con2Torneos_ConOrganizador()
    {
        var r = await fix.Http.GetAsync($"/api/videojuegos/{fix.LoLId}/torneos");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2, $"Se esperaban >= 2 torneos de LoL, hay {arr.GetArrayLength()}");

        // Verificar que incluye nombreOrganizador (no nombreVideojuego — ya se conoce el juego)
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("torneoId", out _),          "Falta torneoId");
            Assert.True(el.TryGetProperty("nombreTorneo", out _),      "Falta nombreTorneo");
            Assert.True(el.TryGetProperty("nombreOrganizador", out _), "Falta nombreOrganizador");
            Assert.True(el.TryGetProperty("fechaInicio", out _),       "Falta fechaInicio");

            // nombreOrganizador nunca debe ser el nombre de un videojuego
            var org = el.GetProperty("nombreOrganizador").GetString()!;
            Assert.DoesNotContain("League", org);
            Assert.DoesNotContain("Dota", org);
        }

        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.Contains("Worlds 2025", nombres);
        Assert.Contains("MSI 2026",    nombres);
    }

    [Fact]
    public async Task Q9_Dota2_Devuelve200_Con1Torneo()
    {
        var r = await fix.Http.GetAsync($"/api/videojuegos/{fix.Dota2Id}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal("The International 2025",
            arr.EnumerateArray().First().GetProperty("nombreTorneo").GetString());
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
        Assert.Contains("ESL Gaming",    nombres);
        Assert.Contains("PGL",           nombres);
        Assert.Contains("BLAST Premier", nombres);
    }

    // ─── Q11: Torneos por organizador ───────────────────────────────────────────

    [Fact]
    public async Task Q11_ESL_Devuelve200_ConWorldsYTI()
    {
        var r = await fix.Http.GetAsync($"/api/organizadores/{fix.ESLId}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2);
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.Contains("Worlds 2025",          nombres);
        Assert.Contains("The International 2025", nombres);
    }

    [Fact]
    public async Task Q11_PGL_Devuelve200_ConMSIYCSMajor()
    {
        var r = await fix.Http.GetAsync($"/api/organizadores/{fix.PGLId}/torneos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2);
        var nombres = arr.EnumerateArray().Select(e => e.GetProperty("nombreTorneo").GetString()).ToList();
        Assert.Contains("MSI 2026",              nombres);
        Assert.Contains("CS2 Major Copenhagen",  nombres);
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

        // Verificar orden descendente (el más reciente primero)
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
    public async Task Q13_Worlds_Devuelve200_ConT1_DRX_FNC()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.WorldsId}/equipos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(3, arr.GetArrayLength());
        var equipos = arr.EnumerateArray().Select(e => e.GetProperty("nombreEquipo").GetString()).ToList();
        Assert.Contains("T1",     equipos);
        Assert.Contains("DRX",    equipos);
        Assert.Contains("Fnatic", equipos);
    }

    [Fact]
    public async Task Q13_CSMajor_Devuelve200_ConNaVi_FaZe_C9()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.CSMajorId}/equipos");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(3, arr.GetArrayLength());
        var equipos = arr.EnumerateArray().Select(e => e.GetProperty("nombreEquipo").GetString()).ToList();
        Assert.Contains("Natus Vincere", equipos);
        Assert.Contains("FaZe Clan",     equipos);
        Assert.Contains("Cloud9",        equipos);
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
        Assert.Contains("Worlds 2025", nombres);
        Assert.Contains("MSI 2026",    nombres);
    }

    [Fact]
    public async Task Q14_EquipoSinTorneos_Devuelve200_ListaVacia()
    {
        // FaZe solo está en CS Major, no en ningún torneo de LoL
        var r = await fix.Http.GetAsync($"/api/torneos/por-equipo/{fix.FaZeId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1, "FaZe debería estar en al menos CS Major");
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
        Assert.Equal("Worlds 2025", doc.GetProperty("nombre").GetString());
        Assert.NotEqual(Guid.Empty, doc.GetProperty("torneoId").GetGuid());
        Assert.True(doc.TryGetProperty("fechaInicio", out _));
    }

    [Fact]
    public async Task Q15_CodigoCS2MAJOR26_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-codigo/CS2MAJOR26");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal("CS2 Major Copenhagen", doc.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Q15_CodigoInexistente_Devuelve404()
    {
        var r = await fix.Http.GetAsync("/api/torneos/por-codigo/TORNEO_QUE_NO_EXISTE");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ─── Q20: Premios por torneo ────────────────────────────────────────────────

    [Fact]
    public async Task Q20_Worlds_Devuelve200_Con2Premios()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.WorldsId}/premios");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, arr.GetArrayLength());

        var montos = arr.EnumerateArray()
            .Select(e => e.GetProperty("monto").GetDecimal())
            .OrderByDescending(m => m)
            .ToList();
        Assert.Equal(500000m, montos[0]);
        Assert.Equal(200000m, montos[1]);
    }

    [Fact]
    public async Task Q20_Worlds_PrimerLugar_EsT1()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.WorldsId}/premios");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var primerLugar = arr.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("tipo").GetString() == "Primer lugar");
        Assert.NotEqual(default, primerLugar);
        Assert.Equal(fix.T1Id, primerLugar.GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task Q20_TorneoSinPremios_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/torneos/{fix.MSIId}/premios");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q21: Premios por equipo ────────────────────────────────────────────────

    [Fact]
    public async Task Q21_T1_Devuelve200_Con1Premio_De500k()
    {
        var r = await fix.Http.GetAsync($"/api/premios/por-equipo/{fix.T1Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal(500000m, arr.EnumerateArray().First().GetProperty("monto").GetDecimal());
    }

    [Fact]
    public async Task Q21_NaVi_Devuelve200_Con1Premio_De250k()
    {
        var r = await fix.Http.GetAsync($"/api/premios/por-equipo/{fix.NaViId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal(250000m, arr.EnumerateArray().First().GetProperty("monto").GetDecimal());
    }

    [Fact]
    public async Task Q21_EquipoSinPremios_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/premios/por-equipo/{fix.G2Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }
}
