using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

/// <summary>
/// Tests de integración para el servicio matches (Q16–Q19) vía gateway :8080.
/// </summary>
[Collection("Gateway")]
public class MatchesTests(GatewayFixture fix, ITestOutputHelper output)
{
    // ─── Q16: Partidas por torneo ───────────────────────────────────────────────

    [Fact]
    public async Task Q16_Worlds_Devuelve200_Con5Partidas()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{fix.WorldsId}");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(5, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q16_CSMajor_Devuelve200_Con3Partidas()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{fix.CSMajorId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(3, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q16_CadaPartida_TieneNombreLocalVisitanteYResultado()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{fix.WorldsId}");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("partidaId", out _),         "Falta partidaId");
            Assert.True(el.TryGetProperty("nombreLocal", out _),       "Falta nombreLocal");
            Assert.True(el.TryGetProperty("nombreVisitante", out _),   "Falta nombreVisitante");
            Assert.True(el.TryGetProperty("resultado", out _),         "Falta resultado");
            Assert.True(el.TryGetProperty("fecha", out _),             "Falta fecha");
        }
    }

    [Fact]
    public async Task Q16_TorneoIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q17: Partidas por equipo ───────────────────────────────────────────────

    [Fact]
    public async Task Q17_T1_Devuelve200_ConAlMenos5Partidas()
    {
        // T1 jugó: 4 en Worlds (vs DRX x3, vs FNC x1) + 1 en MSI = 5 partidas
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.T1Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 5,
            $"T1 debería tener >= 5 partidas, tiene {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q17_NaVi_Devuelve200_Con2Partidas_AmbosResultados()
    {
        // NaVi jugó 2 partidas en CS Major, ambas victorias
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.NaViId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2);

        var resultados = arr.EnumerateArray()
            .Select(e => e.GetProperty("resultado").GetString())
            .ToList();
        Assert.All(resultados, res => Assert.Equal("VICTORIA", res));
    }

    [Fact]
    public async Task Q17_FaZe_TieneVictoriasYDerrotas()
    {
        // FaZe: venció a C9, pero perdió ante NaVi
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.FaZeId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 2);

        var resultados = arr.EnumerateArray()
            .Select(e => e.GetProperty("resultado").GetString())
            .ToList();
        Assert.Contains("VICTORIA", resultados);
        Assert.Contains("DERROTA",  resultados);
    }

    [Fact]
    public async Task Q17_CadaFila_TieneTorneoRivalYResultado()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.T1Id}");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("nombreTorneo", out _), "Falta nombreTorneo");
            Assert.True(el.TryGetProperty("rival", out _),        "Falta rival");
            Assert.True(el.TryGetProperty("resultado", out _),    "Falta resultado");
            Assert.True(el.TryGetProperty("fecha", out _),        "Falta fecha");
            var res = el.GetProperty("resultado").GetString();
            Assert.True(res == "VICTORIA" || res == "DERROTA",
                $"Resultado inesperado: {res}");
        }
    }

    [Fact]
    public async Task Q17_EquipoIdInexistente_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q18: Partidas por fecha ────────────────────────────────────────────────

    [Fact]
    public async Task Q18_Fecha20251015_Devuelve200_Con1Partida()
    {
        var r = await fix.Http.GetAsync("/api/partidas/por-fecha/2025-10-15");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
        // El primer match del seeder es T1 vs DRX ese día
        var local = arr.EnumerateArray().First().GetProperty("nombreLocal").GetString();
        Assert.Equal("T1", local);
    }

    [Fact]
    public async Task Q18_Fecha20260410_Devuelve200_Con1Partida()
    {
        // NaVi vs FaZe en CS Major
        var r = await fix.Http.GetAsync("/api/partidas/por-fecha/2026-04-10");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q18_FechaSinPartidas_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync("/api/partidas/por-fecha/1999-01-01");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ─── Q19: Partidas entre dos equipos (bidireccional) ────────────────────────

    [Fact]
    public async Task Q19_T1_vs_DRX_Devuelve200_ConAlMenos3Partidas()
    {
        // T1 y DRX jugaron 3 veces directamente (oct15, oct20, oct22 en Worlds)
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{fix.T1Id}/{fix.DRXId}");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 3,
            $"Se esperaban >= 3 partidas T1-DRX, hay {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q19_DRX_vs_T1_EsBidireccional_MismoConteo()
    {
        // La doble escritura debe garantizar el mismo resultado en ambas direcciones
        var r1 = await fix.Http.GetAsync($"/api/partidas/entre/{fix.T1Id}/{fix.DRXId}");
        var r2 = await fix.Http.GetAsync($"/api/partidas/entre/{fix.DRXId}/{fix.T1Id}");

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        var arr1 = GatewayFixture.ParseJson(await r1.Content.ReadAsStringAsync());
        var arr2 = GatewayFixture.ParseJson(await r2.Content.ReadAsStringAsync());
        Assert.Equal(arr1.GetArrayLength(), arr2.GetArrayLength());
    }

    [Fact]
    public async Task Q19_NaVi_vs_FaZe_Devuelve200_Con1Partida()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{fix.NaViId}/{fix.FaZeId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q19_T1_vs_G2_NuncaJugaron_Devuelve200_ListaVacia()
    {
        // T1 y G2 no tienen ningún enfrentamiento en el seeder
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{fix.T1Id}/{fix.G2Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q19_EntreEquiposInexistentes_Devuelve200_ListaVacia()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{Guid.NewGuid()}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.GetArrayLength());
    }
}
