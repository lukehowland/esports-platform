using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

[Collection("Gateway")]
public class MatchesTests(GatewayFixture fix, ITestOutputHelper output)
{
    // ─── Q16: Partidas por torneo ───────────────────────────────────────────────

    [Fact]
    public async Task Q16_Worlds_Devuelve200_ConAlMenos10Partidas()
    {
        // WORLDS25 (10 equipos, 4 rondas round-robin + final) genera 21 partidas
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{fix.WorldsId}");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 10, $"WORLDS25 debe tener >= 10 partidas, tiene {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q16_IEM_Devuelve200_ConAlMenos10Partidas()
    {
        // IEM-COL26 (12 equipos, 4 rondas + final) genera 25 partidas
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{fix.IEMId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 10, $"IEM-COL26 debe tener >= 10 partidas, tiene {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q16_CadaPartida_TieneNombreLocalVisitanteYResultado()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-torneo/{fix.WorldsId}");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("partidaId", out _),       "Falta partidaId");
            Assert.True(el.TryGetProperty("nombreLocal", out _),     "Falta nombreLocal");
            Assert.True(el.TryGetProperty("nombreVisitante", out _), "Falta nombreVisitante");
            Assert.True(el.TryGetProperty("resultado", out _),       "Falta resultado");
            Assert.True(el.TryGetProperty("fecha", out _),           "Falta fecha");
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
        // T1 juega en WORLDS25 (5 partidas) + MSI26 + LEC-SUM26
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.T1Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 5,
            $"T1 debería tener >= 5 partidas, tiene {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q17_NAVI_Devuelve200_ConVictoriasYDerrotas()
    {
        // NAVI juega en IEM-COL26 y otros torneos BLAST
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.NAVIId}");
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
    public async Task Q17_FAZE_TieneVictoriasYDerrotas()
    {
        var r = await fix.Http.GetAsync($"/api/partidas/por-equipo/{fix.FAZEId}");
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
    public async Task Q18_Fecha20251015_Devuelve200_ConAlMenos1Partida()
    {
        // WORLDS25 empieza 2025-10-14; ronda 0 cae el 2025-10-15
        var r = await fix.Http.GetAsync("/api/partidas/por-fecha/2025-10-15");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1, "Debe haber partidas de WORLDS25 el 2025-10-15");
    }

    [Fact]
    public async Task Q18_Fecha20260604_IEM_Devuelve200_ConAlMenos1Partida()
    {
        // IEM-COL26 empieza 2026-06-02; el seed genera partidas a partir de 2026-06-04.
        var r = await fix.Http.GetAsync("/api/partidas/por-fecha/2026-06-04");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1, "Debe haber partidas de IEM-COL26 el 2026-06-04");
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
    public async Task Q19_T1_vs_G2_Devuelve200_ConAlMenos3Partidas()
    {
        // T1(idx 0) y G2(idx 1) se enfrentan en rondas 0, 2 y la final de WORLDS25
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{fix.T1Id}/{fix.G2Id}");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 3,
            $"Se esperaban >= 3 partidas T1-G2, hay {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q19_G2_vs_T1_EsBidireccional_MismoConteo()
    {
        var r1 = await fix.Http.GetAsync($"/api/partidas/entre/{fix.T1Id}/{fix.G2Id}");
        var r2 = await fix.Http.GetAsync($"/api/partidas/entre/{fix.G2Id}/{fix.T1Id}");

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        var arr1 = GatewayFixture.ParseJson(await r1.Content.ReadAsStringAsync());
        var arr2 = GatewayFixture.ParseJson(await r2.Content.ReadAsStringAsync());
        Assert.Equal(arr1.GetArrayLength(), arr2.GetArrayLength());
    }

    [Fact]
    public async Task Q19_NAVI_vs_FAZE_Devuelve200_Con1Partida()
    {
        // NAVI(idx 3) y FAZE(idx 4) se enfrentan directamente en IEM-COL26
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{fix.NAVIId}/{fix.FAZEId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Q19_T1_vs_NAVI_NuncaJugaron_Devuelve200_ListaVacia()
    {
        // T1 es equipo LoL, NAVI es CS2 — nunca comparten torneo
        var r = await fix.Http.GetAsync($"/api/partidas/entre/{fix.T1Id}/{fix.NAVIId}");
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
