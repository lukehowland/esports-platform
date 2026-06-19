using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

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
    public async Task Q1_s1mple_NAVI_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-nickname/s1mple");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal("UA",  doc.GetProperty("pais").GetString());
        Assert.Equal("AWP", doc.GetProperty("rol").GetString());
    }

    // ─── Q2: Jugadores por país ─────────────────────────────────────────────────

    [Fact]
    public async Task Q2_PaisKR_Devuelve200_ConAlMenos5Jugadores()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-pais/KR");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(arr.GetArrayLength() >= 5, $"Se esperaban >= 5 jugadores KR, hay {arr.GetArrayLength()}");
    }

    [Fact]
    public async Task Q2_PaisDK_Devuelve200_ConKarriganDeFAZE()
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
        var r = await fix.Http.GetAsync($"/api/equipos/{Guid.NewGuid()}/jugadores");
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
    public async Task Q5_TagNAVI_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/api/equipos/por-tag/NAVI");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.True(doc.GetProperty("nombre").GetString()!.Contains("Natus Vincere"));
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
    public async Task Q6_IntegrantesNAVI_Devuelve200_Con2Jugadores()
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{fix.NAVIId}/integrantes");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.Equal(2, arr.GetArrayLength());
        var nicknames = arr.EnumerateArray().Select(e => e.GetProperty("nickname").GetString()).ToList();
        Assert.Contains("s1mple",     nicknames);
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
        var r = await fix.AdminPost("/api/equipos", new { nombre = "Test Team", tag = uniqueTag, pais = "CO" });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.NotEqual(Guid.Empty, doc.GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task POST_CrearEquipoConCuerpoVacio_Devuelve400()
    {
        var r = await fix.AdminPost("/api/equipos", new { });
        Assert.True(
            r.StatusCode == HttpStatusCode.BadRequest ||
            r.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Se esperaba 400/422 pero fue {r.StatusCode}");
    }

    // ─── RF-03: código de jugador (J-001) + membresías (baja → agente libre → alta) ──

    private async Task<Guid> CrearEquipoAsync()
    {
        var tag = $"M{Guid.NewGuid():N}"[..7].ToUpperInvariant();
        var r = await fix.AdminPost("/api/equipos", new { nombre = $"Membership {tag}", tag, pais = "CO" });
        r.EnsureSuccessStatusCode();
        return GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync()).GetProperty("equipoId").GetGuid();
    }

    private async Task<(Guid Id, string Codigo)> CrearJugadorAsync(Guid equipoId, string rol = "FLEX")
    {
        var nick = $"mtest{Guid.NewGuid():N}"[..14];
        var r = await fix.AdminPost($"/api/equipos/{equipoId}/jugadores",
            new { nickname = nick, nombre = "Test Player", pais = "CO", rol, email = $"{nick}@test.gg", telefono = "+1-555-0000" });
        r.EnsureSuccessStatusCode();
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        return (doc.GetProperty("jugadorId").GetGuid(), doc.GetProperty("codigo").GetString()!);
    }

    private async Task<List<JsonElement>> MembresiasAsync(Guid jugadorId)
    {
        var r = await fix.Http.GetAsync($"/api/jugadores/{jugadorId}/membresias");
        r.EnsureSuccessStatusCode();
        return GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync()).EnumerateArray().ToList();
    }

    private async Task<List<Guid>> IntegrantesAsync(Guid equipoId)
    {
        var r = await fix.Http.GetAsync($"/api/equipos/{equipoId}/integrantes");
        r.EnsureSuccessStatusCode();
        return GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync())
            .EnumerateArray().Select(e => e.GetProperty("jugadorId").GetGuid()).ToList();
    }

    // Verifica el cache denormalizado del equipo activo en jugadores (por id) y jugadores_por_nickname.
    private async Task AssertEquipoActivoAsync(Guid jugadorId, Guid equipoEsperado)
    {
        var byId = GatewayFixture.ParseJson(await (await fix.Http.GetAsync($"/api/jugadores/{jugadorId}")).Content.ReadAsStringAsync());
        Assert.Equal(equipoEsperado, byId.GetProperty("equipoId").GetGuid());

        var nick = byId.GetProperty("nickname").GetString();
        var byNick = GatewayFixture.ParseJson(await (await fix.Http.GetAsync($"/api/jugadores/por-nickname/{nick}")).Content.ReadAsStringAsync());
        Assert.Equal(equipoEsperado, byNick.GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task RF03_AltaJugador_AsignaCodigoConFormatoJ()
    {
        var equipo = await CrearEquipoAsync();
        var (_, codigo) = await CrearJugadorAsync(equipo);
        Assert.Matches(@"^J-\d+$", codigo);
    }

    [Fact]
    public async Task RF03_PorCodigo_DevuelveElJugador_YInexistente404()
    {
        var equipo = await CrearEquipoAsync();
        var (id, codigo) = await CrearJugadorAsync(equipo);

        var r = await fix.Http.GetAsync($"/api/jugadores/por-codigo/{codigo}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(id, GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync()).GetProperty("jugadorId").GetGuid());

        var r404 = await fix.Http.GetAsync("/api/jugadores/por-codigo/J-99999999");
        Assert.Equal(HttpStatusCode.NotFound, r404.StatusCode);
    }

    [Fact]
    public async Task RF03_AltaCreaMembresiaActiva()
    {
        var equipo = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipo);

        var membresias = await MembresiasAsync(id);
        Assert.Single(membresias);
        Assert.True(membresias[0].GetProperty("activa").GetBoolean());
        Assert.Equal(equipo, membresias[0].GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task RF03_Liberar_DejaAgenteLibre_YPreservaHistorial()
    {
        var equipo = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipo);

        var lib = await fix.AdminPost($"/api/jugadores/{id}/liberar", new { });
        Assert.Equal(HttpStatusCode.NoContent, lib.StatusCode);

        // Sale del roster del equipo
        Assert.DoesNotContain(id, await IntegrantesAsync(equipo));

        // Queda agente libre (equipoId null)
        var jr = await fix.Http.GetAsync($"/api/jugadores/{id}");
        var jdoc = GatewayFixture.ParseJson(await jr.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, jdoc.GetProperty("equipoId").ValueKind);

        // El historial se preserva: la membresía sigue, ahora cerrada (no activa)
        var membresias = await MembresiasAsync(id);
        Assert.Single(membresias);
        Assert.False(membresias[0].GetProperty("activa").GetBoolean());
    }

    [Fact]
    public async Task RF03_FicharAgenteLibre_LoSumaAlRoster_YAbreMembresia()
    {
        var equipoA = await CrearEquipoAsync();
        var equipoB = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipoA);

        await fix.AdminPost($"/api/jugadores/{id}/liberar", new { });
        var fichar = await fix.AdminPost($"/api/jugadores/{id}/asignar", new { equipoDestinoId = equipoB });
        Assert.Equal(HttpStatusCode.NoContent, fichar.StatusCode);

        Assert.Contains(id, await IntegrantesAsync(equipoB));
        var membresias = await MembresiasAsync(id);
        Assert.Equal(2, membresias.Count);
        Assert.Single(membresias, m => m.GetProperty("activa").GetBoolean());
        Assert.Equal(equipoB, membresias.Single(m => m.GetProperty("activa").GetBoolean()).GetProperty("equipoId").GetGuid());
        await AssertEquipoActivoAsync(id, equipoB);
    }

    [Fact]
    public async Task RF03_TraspasoAtomicoAdmin_CierraVieja_YAbreNueva()
    {
        var equipoA = await CrearEquipoAsync();
        var equipoB = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipoA);

        // Asignar (admin) un jugador con equipo activo => traspaso atómico, sin liberar antes
        var r = await fix.AdminPost($"/api/jugadores/{id}/asignar", new { equipoDestinoId = equipoB });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);

        Assert.DoesNotContain(id, await IntegrantesAsync(equipoA));
        Assert.Contains(id, await IntegrantesAsync(equipoB));
        var membresias = await MembresiasAsync(id);
        Assert.Equal(2, membresias.Count);
        Assert.Single(membresias, m => m.GetProperty("activa").GetBoolean());
        await AssertEquipoActivoAsync(id, equipoB);
    }

    [Fact]
    public async Task RF03_AsignarAlMismoEquipo_Devuelve409()
    {
        var equipo = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipo);

        var r = await fix.AdminPost($"/api/jugadores/{id}/asignar", new { equipoDestinoId = equipo });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task RF03_CapitanFichaJugadorConEquipoActivo_Devuelve409_RequiereLiberar()
    {
        // Jugador con equipo activo en A; capitán de FNC intenta ficharlo a FNC.
        var equipoA = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipoA);

        var capToken = await fix.LoginAsync("cap_fnc", "CapDemo2024");
        var r = await fix.AuthedPost($"/api/jugadores/{id}/asignar", new { equipoDestinoId = fix.FNCId }, capToken);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task RF03_CapitanSobreEquipoAjeno_Devuelve403()
    {
        var equipoA = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipoA);

        // Capitán de FNC intenta fichar hacia un equipo que no es el suyo
        var capToken = await fix.LoginAsync("cap_fnc", "CapDemo2024");
        var r = await fix.AuthedPost($"/api/jugadores/{id}/asignar", new { equipoDestinoId = equipoA }, capToken);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task RF03_LiberarSinToken_Devuelve401_YFan403()
    {
        var equipo = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipo);

        var sin = await fix.Http.PostAsync($"/api/jugadores/{id}/liberar",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, sin.StatusCode);

        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");
        var fan = await fix.AuthedPost($"/api/jugadores/{id}/liberar", new { }, fanToken);
        Assert.Equal(HttpStatusCode.Forbidden, fan.StatusCode);
    }

    // ─── RF-01: email/telefono + editar/eliminar jugador ────────────────────────

    [Fact]
    public async Task RF01_AltaJugador_GuardaEmailYTelefono()
    {
        var equipo = await CrearEquipoAsync();
        var (_, codigo) = await CrearJugadorAsync(equipo);

        var r = await fix.Http.GetAsync($"/api/jugadores/por-codigo/{codigo}");
        var doc = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(doc.GetProperty("email").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(doc.GetProperty("telefono").GetString()));
    }

    [Fact]
    public async Task RF01_EditarJugador_ActualizaContacto()
    {
        var equipo = await CrearEquipoAsync();
        var (id, codigo) = await CrearJugadorAsync(equipo);

        var put = await fix.AdminPut($"/api/jugadores/{id}", new { nombre = "Nuevo Nombre", email = "nuevo@test.gg", telefono = "+1-555-9999" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var doc = GatewayFixture.ParseJson(await (await fix.Http.GetAsync($"/api/jugadores/{id}")).Content.ReadAsStringAsync());
        Assert.Equal("Nuevo Nombre", doc.GetProperty("nombre").GetString());
        Assert.Equal("nuevo@test.gg", doc.GetProperty("email").GetString());
        Assert.Equal("+1-555-9999", doc.GetProperty("telefono").GetString());

        var porCodigo = GatewayFixture.ParseJson(
            await (await fix.Http.GetAsync($"/api/jugadores/por-codigo/{codigo}")).Content.ReadAsStringAsync());
        Assert.Equal("Nuevo Nombre", porCodigo.GetProperty("nombre").GetString());
        Assert.Equal("nuevo@test.gg", porCodigo.GetProperty("email").GetString());
    }

    [Fact]
    public async Task RF01_EliminarJugador_AgenteLibreOK_ConEquipo409_NoAdmin403()
    {
        var equipo = await CrearEquipoAsync();
        var (id, _) = await CrearJugadorAsync(equipo);

        // Con equipo activo → 409
        var conEquipo = await fix.AdminDelete($"/api/jugadores/{id}");
        Assert.Equal(HttpStatusCode.Conflict, conEquipo.StatusCode);

        // Liberar → agente libre → admin elimina → 204
        await fix.AdminPost($"/api/jugadores/{id}/liberar", new { });
        var del = await fix.AdminDelete($"/api/jugadores/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // No-admin → 403
        var (otro, _) = await CrearJugadorAsync(equipo);
        var fan = await fix.LoginAsync("fan_demo", "FanDemo2024");
        var r = await fix.AuthedDelete($"/api/jugadores/{otro}", fan);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    // ─── RF-02: CRUD admin de equipos ───────────────────────────────────────────

    [Fact]
    public async Task RF02_EditarEquipoSinRoster_Devuelve200()
    {
        var equipo = await CrearEquipoAsync();
        var original = GatewayFixture.ParseJson(
            await (await fix.Http.GetAsync($"/api/equipos/{equipo}")).Content.ReadAsStringAsync());
        var tagAnterior = original.GetProperty("tag").GetString()!;
        var nuevoTag = $"E{Guid.NewGuid():N}"[..7].ToUpperInvariant();
        var put = await fix.AdminPut($"/api/equipos/{equipo}", new { nombre = "Equipo Editado", tag = nuevoTag, pais = "AR" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Equipo Editado", GatewayFixture.ParseJson(await put.Content.ReadAsStringAsync()).GetProperty("nombre").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await fix.Http.GetAsync($"/api/equipos/por-tag/{tagAnterior}")).StatusCode);
        var nuevoLookup = await fix.Http.GetAsync($"/api/equipos/por-tag/{nuevoTag}");
        Assert.Equal(HttpStatusCode.OK, nuevoLookup.StatusCode);
        Assert.Equal(equipo,
            GatewayFixture.ParseJson(await nuevoLookup.Content.ReadAsStringAsync()).GetProperty("equipoId").GetGuid());
    }

    [Fact]
    public async Task RF02_EliminarEquipo_SinRosterOK_ConRoster409()
    {
        var vacio = await CrearEquipoAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await fix.AdminDelete($"/api/equipos/{vacio}")).StatusCode);

        var conRoster = await CrearEquipoAsync();
        await CrearJugadorAsync(conRoster);
        Assert.Equal(HttpStatusCode.Conflict, (await fix.AdminDelete($"/api/equipos/{conRoster}")).StatusCode);
        var put = await fix.AdminPut($"/api/equipos/{conRoster}", new { nombre = "X", tag = "XYZ", pais = "AR" });
        Assert.Equal(HttpStatusCode.Conflict, put.StatusCode);
    }

    [Fact]
    public async Task RF02_EquipoCrud_SinToken401_NoAdmin403()
    {
        var equipo = await CrearEquipoAsync();

        var sin = await fix.Http.DeleteAsync($"/api/equipos/{equipo}");
        Assert.Equal(HttpStatusCode.Unauthorized, sin.StatusCode);

        var fan = await fix.LoginAsync("fan_demo", "FanDemo2024");
        var r = await fix.AuthedDelete($"/api/equipos/{equipo}", fan);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }
}
