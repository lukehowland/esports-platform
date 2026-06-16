using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

/// <summary>
/// Tests de integración para el servicio ranking (Q7, Q22–Q24) vía gateway :8080.
/// Nota: el ranking es event-driven (consistencia eventual). Los tests asumen
/// que el seeder ya corrió Y que el ranking procesó todos los eventos.
/// </summary>
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
        var r = await fix.Http.GetAsync("/api/ranking/equipos?top=10");
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
    public async Task Q22_T1_TieneMasVictorias_QueCualquierOtro()
    {
        // T1 ganó 4 partidas en el seeder (más que cualquier otro equipo)
        var r = await fix.Http.GetAsync("/api/ranking/victorias?top=10");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var primero = arr.EnumerateArray().First();
        Assert.Equal(fix.T1Id, primero.GetProperty("equipoId").GetGuid());
        Assert.True(primero.GetProperty("totalVictorias").GetInt64() >= 4,
            $"T1 debería tener >= 4 victorias, tiene {primero.GetProperty("totalVictorias").GetInt64()}");
    }

    [Fact]
    public async Task Q22_NaVi_TieneAlMenos2Victorias()
    {
        var r = await fix.Http.GetAsync("/api/ranking/victorias?top=10");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var navi = arr.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("equipoId").GetGuid() == fix.NaViId);
        Assert.NotEqual(default, navi);
        Assert.True(navi.GetProperty("totalVictorias").GetInt64() >= 2,
            "NaVi ganó 2 partidas en CS Major");
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
    public async Task Q23_JugadoresFNC_AparecenConAlMenos2Torneos()
    {
        // Fnatic (FNC) participó en 3 torneos (Worlds, MSI, BLAST) — sus jugadores tienen totalTorneos >= 3
        var r = await fix.Http.GetAsync("/api/ranking/jugadores?top=20");
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        var conAlMenos3 = arr.EnumerateArray()
            .Where(e => e.GetProperty("totalTorneos").GetInt64() >= 3)
            .ToList();
        Assert.True(conAlMenos3.Count >= 2,
            "Debería haber al menos 2 jugadores con >= 3 torneos (Humanoid y Upset de Fnatic)");
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
    public async Task Q24_T1_Worlds_Devuelve200_ConEstadisticasCorrectas()
    {
        // T1 en Worlds: ganó 3 (vs DRX en oct15, vs FNC en oct17, final oct22)
        //              perdió 1 (vs DRX en oct20)
        //              total: 4 partidas
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.T1Id}/torneo/{fix.WorldsId}");
        output.WriteLine(await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        Assert.Equal(fix.T1Id,     doc.GetProperty("equipoId").GetGuid());
        Assert.Equal(fix.WorldsId, doc.GetProperty("torneoId").GetGuid());

        var victorias      = doc.GetProperty("victorias").GetInt64();
        var derrotas       = doc.GetProperty("derrotas").GetInt64();
        var partidasJugadas = doc.GetProperty("partidasJugadas").GetInt64();

        Assert.True(victorias >= 3,       $"T1 debería tener >= 3 victorias en Worlds, tiene {victorias}");
        Assert.True(derrotas >= 1,        $"T1 debería tener >= 1 derrota en Worlds, tiene {derrotas}");
        Assert.Equal(victorias + derrotas, partidasJugadas);
    }

    [Fact]
    public async Task Q24_NaVi_CSMajor_Devuelve200_SoloVictorias()
    {
        // NaVi en CS Major: ganó 2, perdió 0
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.NaViId}/torneo/{fix.CSMajorId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        Assert.True(doc.GetProperty("victorias").GetInt64() >= 2, "NaVi debe tener >= 2 victorias en CS Major");
        Assert.Equal(0L, doc.GetProperty("derrotas").GetInt64());
    }

    [Fact]
    public async Task Q24_PartidaJugadasEsConsistenteConVictoriasYDerrotas()
    {
        // Verifica la invariante: partidas_jugadas = victorias + derrotas
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.FaZeId}/torneo/{fix.CSMajorId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        var v = doc.GetProperty("victorias").GetInt64();
        var d = doc.GetProperty("derrotas").GetInt64();
        var pj = doc.GetProperty("partidasJugadas").GetInt64();
        Assert.Equal(v + d, pj);
    }

    [Fact]
    public async Task Q24_EquipoQueNuncaJugoEnTorneo_Devuelve200_Ceros()
    {
        // G2 nunca jugó en Worlds
        var r = await fix.Http.GetAsync($"/api/stats/equipo/{fix.G2Id}/torneo/{fix.WorldsId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());

        Assert.Equal(0L, doc.GetProperty("victorias").GetInt64());
        Assert.Equal(0L, doc.GetProperty("derrotas").GetInt64());
        Assert.Equal(0L, doc.GetProperty("partidasJugadas").GetInt64());
    }
}
