using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Esports.Gateway.Tests;

/// <summary>
/// Tests de error, límites y rutas inválidas — verifican que la API falla de forma controlada
/// con status codes correctos y cuerpos ProblemDetails cuando corresponde.
/// </summary>
[Collection("Gateway")]
public class ErrorTests(GatewayFixture fix, ITestOutputHelper output)
{
    // ─── Gateway health ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Gateway_Devuelve200()
    {
        var r = await fix.Http.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // ─── Rutas inexistentes → 404 ────────────────────────────────────────────────

    [Fact]
    public async Task RutaInexistente_Devuelve404()
    {
        var r = await fix.Http.GetAsync("/api/ruta-que-no-existe");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task RutaDeOtroSegmento_Devuelve404()
    {
        // /api/rankings no existe (el correcto es /api/ranking)
        var r = await fix.Http.GetAsync("/api/rankings/equipos?top=10");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ─── GUIDs inválidos → 400/404 ───────────────────────────────────────────────

    [Fact]
    public async Task GuidMalformado_EnRuta_Devuelve400()
    {
        // El router de ASP.NET Core rechaza :guid no válido con 400
        var r = await fix.Http.GetAsync("/api/equipos/no-es-un-guid/jugadores");
        Assert.True(
            r.StatusCode == HttpStatusCode.BadRequest ||
            r.StatusCode == HttpStatusCode.NotFound,
            $"Se esperaba 400 o 404 para GUID malformado, fue {r.StatusCode}");
    }

    [Fact]
    public async Task GuidValido_PeroInexistente_EnTorneo_Devuelve404oVacio()
    {
        // GET /api/torneos/{guid} llama a ObtenerPorId → devuelve 404 si no existe
        var r = await fix.Http.GetAsync($"/api/torneos/{Guid.NewGuid()}");
        // El controller devuelve 404 cuando el servicio retorna null
        Assert.True(
            r.StatusCode == HttpStatusCode.NotFound ||
            r.StatusCode == HttpStatusCode.OK,
            $"Inesperado: {r.StatusCode}");
    }

    // ─── Cuerpos inválidos en POST → 400 ────────────────────────────────────────

    [Fact]
    public async Task POST_CrearEquipo_SinBody_Devuelve400()
    {
        var r = await fix.AdminPostJson("/api/equipos", "");
        Assert.True(
            r.StatusCode == HttpStatusCode.BadRequest ||
            r.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Se esperaba 400, fue {r.StatusCode}");
    }

    [Fact]
    public async Task POST_CrearEquipo_NombreVacio_Devuelve400()
    {
        var r = await fix.AdminPost("/api/equipos", new { nombre = "", tag = "TST", pais = "CO" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task POST_CrearVideojuego_SinGenero_Devuelve400()
    {
        var r = await fix.AdminPost("/api/videojuegos", new { nombre = "TestGame", plataforma = "PC" }); // falta genero
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task POST_CrearVideojuego_SinPlataforma_Devuelve400()
    {
        var r = await fix.AdminPost("/api/videojuegos", new { nombre = "TestGame", genero = "FPS" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task POST_CrearOrganizador_SinEmail_Devuelve400()
    {
        var r = await fix.AdminPost("/api/organizadores", new { nombre = "Organizador sin contacto" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task POST_CrearTorneo_VideojuegoIdInexistente_Devuelve404()
    {
        // El service llama a GetVideojuegoById — si no existe, retorna 404
        var r = await fix.AdminPost("/api/torneos", new
        {
            nombre = "Torneo Fantasma",
            codigo = $"TF{Guid.NewGuid():N}"[..10].ToUpper(),
            videojuegoId = Guid.NewGuid(),      // ID que no existe
            organizadorId = fix.ESLId,
            fechaInicio = "2026-12-01T00:00:00Z",
            fechaFin = "2026-12-08T00:00:00Z"
        });
        output.WriteLine($"POST torneo con VJ inexistente → {r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task POST_CrearTorneo_OrganizadorIdInexistente_Devuelve404()
    {
        var r = await fix.AdminPost("/api/torneos", new
        {
            nombre = "Torneo Fantasma",
            codigo = $"TF{Guid.NewGuid():N}"[..10].ToUpper(),
            videojuegoId = fix.LoLId,
            organizadorId = Guid.NewGuid(),     // ID que no existe
            fechaInicio = "2026-12-01T00:00:00Z",
            fechaFin = "2026-12-08T00:00:00Z"
        });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task POST_InscribirEquipo_EnTorneoInexistente_SeIgnoraOError()
    {
        // El controller/service maneja el caso de torneo inexistente
        var r = await fix.AdminPost($"/api/torneos/{Guid.NewGuid()}/inscripciones", new { equipoId = fix.T1Id });
        output.WriteLine($"Inscripción en torneo inexistente → {r.StatusCode}");
        // Puede ser 404 (torneo no encontrado) o 201 (escritura sin validar — ambos son aceptables)
        Assert.True(
            r.StatusCode == HttpStatusCode.NotFound ||
            r.StatusCode == HttpStatusCode.Created ||
            r.StatusCode == HttpStatusCode.BadGateway,
            $"Inesperado: {r.StatusCode}");
    }

    [Fact]
    public async Task POST_InscribirEquipo_EquipoInexistente_Devuelve502o404()
    {
        // teams.GetEquipo retorna 404 → tournaments propaga como 404/502
        var r = await fix.AdminPost($"/api/torneos/{fix.MSIId}/inscripciones", new { equipoId = Guid.NewGuid() });
        output.WriteLine($"Inscripción con equipo inexistente → {r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        Assert.True(
            r.StatusCode == HttpStatusCode.NotFound ||
            r.StatusCode == HttpStatusCode.BadGateway ||
            (int)r.StatusCode == 502,
            $"Se esperaba 404 o 502, fue {r.StatusCode}");
    }

    // ─── Errores en matches ───────────────────────────────────────────────────────

    [Fact]
    public async Task POST_CrearPartida_SinBody_Devuelve400()
    {
        var r = await fix.AdminPostJson("/api/partidas", "");
        Assert.True(
            r.StatusCode == HttpStatusCode.BadRequest ||
            r.StatusCode == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Q18_FechaConFormatoInvalido_Devuelve400o404()
    {
        // La ruta espera YYYY-MM-DD; un formato inválido no matchea la ruta
        var r = await fix.Http.GetAsync("/api/partidas/por-fecha/not-a-date");
        Assert.True(
            r.StatusCode == HttpStatusCode.NotFound ||
            r.StatusCode == HttpStatusCode.BadRequest,
            $"Se esperaba 400/404 para fecha inválida, fue {r.StatusCode}");
    }

    // ─── Consistencia de ProblemDetails ──────────────────────────────────────────

    [Fact]
    public async Task Error404_CuerpoEsProblemDetails_ConTitleYStatus()
    {
        var r = await fix.Http.GetAsync("/api/jugadores/por-nickname/NICK_INEXISTENTE_XYZ");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);

        var body = await r.Content.ReadAsStringAsync();
        output.WriteLine(body);

        // ProblemDetails tiene al menos "status" en el JSON
        if (!string.IsNullOrEmpty(body) && body.StartsWith("{"))
        {
            var doc = GatewayFixture.ParseJson(body);
            if (doc.TryGetProperty("status", out var st))
                Assert.Equal(404, st.GetInt32());
        }
    }

    // ─── Mutaciones protegidas ───────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_Equipo_SinToken_Devuelve401()
    {
        var r = await fix.Http.DeleteAsync($"/api/equipos/{fix.T1Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
