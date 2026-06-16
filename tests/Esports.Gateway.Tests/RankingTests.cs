using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

[Collection("Gateway")]
public class RankingTests(GatewayFixture fix, ITestOutputHelper output)
{
    // ─── Q7: Ranking de equipos por torneos jugados ──────────────────────────────

    [Fact]
    public async Task Q7_Top10_Devuelve200_ListaNoVacia()
    {
        var r = await fix.Http.GetAsync("/api/ranking/equipos?top=10");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() > 0, "El ranking de equipos no debe estar vacío");
    }

    [Fact]
    public async Task Q7_Top10_T1_TieneAlMenos2Torneos()
    {
        var r = await fix.Http.GetAsync("/api/ranking/equipos?top=20");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var t1Entry = arr.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("equipoId").GetGuid() == fix.T1Id);
        Assert.NotEqual(default, t1Entry);
        Assert.True(t1Entry.GetProperty("totalTorneos").GetInt64() >= 2,
            $"T1 debería tener >= 2 torneos, tiene {t1Entry.GetProperty("totalTorneos").GetInt64()}");
    }

    [Fact]
    public async Task Q7_Top1_DevuelveExactamente1Elemento()
    {
        var r = await fix.Http.GetAsync("/api/ranking/equipos?top=1");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q7_CadaElemento_TieneEquipoIdYTotalTorneos()
    {
        var r = await fix.Http.GetAsync("/api/ranking/equipos?top=10");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        foreach (var el in arr.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("equipoId", out var id),        "Falta equipoId");
            Assert.True(el.TryGetProperty("totalTorneos", out var total), "Falta totalTorneos");
            Assert.NotEqual(Guid.Empty, id.GetGuid());
            Assert.True(total.GetInt64() > 0, "totalTorneos debe ser > 0");
        }
    }

    [Fact]
    public async Task Q7_OrdenDescendente_PorTotalTorneos()
    {
        var r = await fix.Http.GetAsync("/api/ranking/equipos?top=10");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var totales = arr.EnumerateArray()
            .Select(e => e.GetProperty("totalTorneos").GetInt64())
            .ToList();
        for (int i = 1; i < totales.Count; i++)
            Assert.True(totales[i - 1] >= totales[i],
                $"Q7 no está en orden descendente: {totales[i-1]} < {totales[i]}");
    }

    // ─── Q22: Ranking de victorias ──────────────────────────────────────────────

    [Fact]
    public async Task Q22_Top10_Devuelve200_ListaNoVacia()
    {
        var r = await fix.Http.GetAsync("/api/ranking/victorias?top=10");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() > 0, "El ranking de victorias no debe estar vacío");
    }

    [Fact]
    public async Task Q22_T1_TieneAlMenos4Victorias()
    {
        // T1 gana en WORLDS25 (3), MSI26 (2), LEC-SUM26 (0) = al menos 4 victorias totales
        var r = await fix.Http.GetAsync("/api/ranking/victorias?top=50");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var t1 = arr.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("equipoId").GetGuid() == fix.T1Id);
        Assert.NotEqual(default, t1);
        Assert.True(t1.GetProperty("totalVictorias").GetInt64() >= 4,
            $"T1 debería tener >= 4 victorias, tiene {t1.GetProperty("totalVictorias").GetInt64()}");
    }

    [Fact]
    public async Task Q22_NAVI_TieneAlMenos1Victoria()
    {
        var r = await fix.Http.GetAsync("/api/ranking/victorias?top=50");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var navi = arr.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("equipoId").GetGuid() == fix.NAVIId);
        Assert.NotEqual(default, navi);
        Assert.True(navi.GetProperty("totalVictorias").GetInt64() >= 1,
            "NAVI debe tener al menos 1 victoria");
    }

    [Fact]
    public async Task Q22_OrdenDescendente_PorTotalVictorias()
    {
        var r = await fix.Http.GetAsync("/api/ranking/victorias?top=10");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var victorias = arr.EnumerateArray()
            .Select(e => e.GetProperty("totalVictorias").GetInt64())
            .ToList();
        for (int i = 1; i < victorias.Count; i++)
            Assert.True(victorias[i - 1] >= victorias[i],
                $"Q22 no está en orden descendente en posición {i}");
    }

    // ─── Q23: Ranking de jugadores por torneos ──────────────────────────────────

    [Fact]
    public async Task Q23_Top10_Devuelve200_ListaNoVacia()
    {
        var r = await fix.Http.GetAsync("/api/ranking/jugadores?top=10");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() > 0, "El ranking de jugadores no debe estar vacío");
    }

    [Fact]
    public async Task Q23_HayJugadoresConAlMenos3Torneos()
    {
        // Equipos como VAL_G2 (5 torneos VAL), CS_VIT (4 torneos CS) aportan jugadores con >= 3
        var r = await fix.Http.GetAsync("/api/ranking/jugadores?top=20");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var conAlMenos3 = arr.EnumerateArray()
            .Where(e => e.GetProperty("totalTorneos").GetInt64() >= 3)
            .ToList();
        Assert.True(conAlMenos3.Count >= 2,
            "Debe haber al menos 2 jugadores con >= 3 torneos");
    }

    [Fact]
    public async Task Q23_Top1_DevuelveExactamente1Elemento()
    {
        var r = await fix.Http.GetAsync("/api/ranking/jugadores?top=1");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(1, arr.GetArrayLength());
    }

    [Fact]
    public async Task Q23_OrdenDescendente_PorTotalTorneos()
    {
        var r = await fix.Http.GetAsync("/api/ranking/jugadores?top=10");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var totales = arr.EnumerateArray()
            .Select(e => e.GetProperty("totalTorneos").GetInt64())
            .ToList();
        for (int i = 1; i < totales.Count; i++)
            Assert.True(totales[i - 1] >= totales[i],
                $"Q23 no está en orden descendente en posición {i}");
    }

    // ─── Q24: Stats de equipo por torneo ────────────────────────────────────────

    [Fact]
    public async Task Q24_T1_Worlds_Devuelve200_ConAlMenos3VictoriasY1Derrota()
    {
        // T1 en WORLDS25: gana en rounds 2, 3 y final; pierde en rounds 0 y 1 = 3V 2D
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.T1Id}/torneo/{fix.WorldsId}");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        Assert.Equal(fix.T1Id,     doc.GetProperty("equipoId").GetGuid());
        Assert.Equal(fix.WorldsId, doc.GetProperty("torneoId").GetGuid());

        var victorias      = doc.GetProperty("victorias").GetInt64();
        var derrotas       = doc.GetProperty("derrotas").GetInt64();
        var partidasJugadas = doc.GetProperty("partidasJugadas").GetInt64();

        Assert.True(victorias >= 3,      $"T1 debería tener >= 3 victorias en Worlds, tiene {victorias}");
        Assert.True(derrotas >= 1,       $"T1 debería tener >= 1 derrota en Worlds, tiene {derrotas}");
        Assert.Equal(victorias + derrotas, partidasJugadas);
    }

    [Fact]
    public async Task Q24_NAVI_IEM_Devuelve200_ConVictoriasYDerrotas()
    {
        // NAVI en IEM-COL26: gana en round 1 vs FAZE; pierde en rounds 0, 2, 3
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.NAVIId}/torneo/{fix.IEMId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        Assert.True(doc.GetProperty("victorias").GetInt64() >= 1, "NAVI debe tener >= 1 victoria en IEM");
        Assert.True(doc.GetProperty("derrotas").GetInt64() >= 1,  "NAVI debe tener >= 1 derrota en IEM");
    }

    [Fact]
    public async Task Q24_PartidaJugadasEsConsistenteConVictoriasYDerrotas()
    {
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.FAZEId}/torneo/{fix.IEMId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var v  = doc.GetProperty("victorias").GetInt64();
        var d  = doc.GetProperty("derrotas").GetInt64();
        var pj = doc.GetProperty("partidasJugadas").GetInt64();
        Assert.Equal(v + d, pj);
    }

    [Fact]
    public async Task Q24_EquipoQueNuncaJugoEnTorneo_Devuelve200_Ceros()
    {
        // T1 (LoL) nunca jugó en IEM-COL26 (CS2)
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.T1Id}/torneo/{fix.IEMId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        Assert.Equal(0L, doc.GetProperty("victorias").GetInt64());
        Assert.Equal(0L, doc.GetProperty("derrotas").GetInt64());
        Assert.Equal(0L, doc.GetProperty("partidasJugadas").GetInt64());
    }
}
