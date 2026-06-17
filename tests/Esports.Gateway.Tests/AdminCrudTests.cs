using System.Net;
using Xunit;

namespace Esports.Gateway.Tests;

/// <summary>
/// CRUD de administrador sobre catálogos (organizadores, videojuegos) y usuarios.
/// Cubre el ciclo completo crear → editar → eliminar y la semántica block-on-dependents:
/// una entidad con torneos asociados no puede editarse ni eliminarse (409).
/// </summary>
[Collection("Gateway")]
public class AdminCrudTests(GatewayFixture fix)
{
    // ─── Organizadores: ciclo completo ──────────────────────────────────────────

    [Fact]
    public async Task Organizador_CrearEditarEliminar_FuncionaSinDependencias()
    {
        // Crear
        using var crear = await fix.AdminPost("/api/organizadores", new
        {
            nombre = $"CRUD Org {Guid.NewGuid():N}"[..20]
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var id = GatewayFixture.ParseJson(await crear.Content.ReadAsStringAsync())
            .GetProperty("organizadorId").GetGuid();

        // Editar (sin torneos → 200)
        var nuevoNombre = $"CRUD Org Editado {Guid.NewGuid():N}"[..26];
        using var editar = await fix.AdminPut($"/api/organizadores/{id}", new { nombre = nuevoNombre });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        // Confirmar el cambio (Q10 lee de organizadores_lista)
        using var lista = await fix.Http.GetAsync("/api/organizadores");
        var nombres = GatewayFixture.ParseJson(await lista.Content.ReadAsStringAsync())
            .EnumerateArray().Select(e => e.GetProperty("nombre").GetString());
        Assert.Contains(nuevoNombre, nombres);

        // Eliminar → 204
        using var eliminar = await fix.AdminDelete($"/api/organizadores/{id}");
        Assert.Equal(HttpStatusCode.NoContent, eliminar.StatusCode);

        // Ya no existe → 404
        using var verificar = await fix.Http.GetAsync($"/api/organizadores/{id}");
        Assert.Equal(HttpStatusCode.NotFound, verificar.StatusCode);
    }

    [Fact]
    public async Task Organizador_ConTorneos_NoSePuedeEliminar_Devuelve409()
    {
        // ESL FACEIT Group organiza IEM → tiene torneos
        using var r = await fix.AdminDelete($"/api/organizadores/{fix.ESLId}");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Organizador_ConTorneos_NoSePuedeEditar_Devuelve409()
    {
        using var r = await fix.AdminPut($"/api/organizadores/{fix.ESLId}", new { nombre = "ESL Renombrado" });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Organizador_EditarInexistente_Devuelve404()
    {
        using var r = await fix.AdminPut($"/api/organizadores/{Guid.NewGuid()}", new { nombre = "Fantasma" });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Organizador_OrganizadorNoPuedeEditar_Devuelve403()
    {
        var riotToken = await fix.LoginAsync("org_riot", "OrgDemo2024");
        using var r = await fix.AuthedPut($"/api/organizadores/{fix.ESLId}", new { nombre = "Hack" }, riotToken);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Organizador_EditarSinToken_Devuelve401()
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/organizadores/{fix.ESLId}")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { nombre = "Anon" }),
        };
        using var r = await fix.Http.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // ─── Videojuegos: ciclo completo ────────────────────────────────────────────

    [Fact]
    public async Task Videojuego_CrearEditarEliminar_FuncionaSinDependencias()
    {
        // Crear
        using var crear = await fix.AdminPost("/api/videojuegos", new
        {
            nombre = $"CRUD Game {Guid.NewGuid():N}"[..22],
            genero = "TESTGENA"
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var id = GatewayFixture.ParseJson(await crear.Content.ReadAsStringAsync())
            .GetProperty("videojuegoId").GetGuid();

        // Editar cambiando el género (delete viejo + insert nuevo en videojuegos_por_genero)
        var nuevoNombre = $"CRUD Game Editado {Guid.NewGuid():N}"[..28];
        using var editar = await fix.AdminPut($"/api/videojuegos/{id}", new
        {
            nombre = nuevoNombre,
            genero = "TESTGENB"
        });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        // Aparece en el nuevo género…
        using var nuevoGen = await fix.Http.GetAsync("/api/videojuegos/por-genero/TESTGENB");
        var enB = GatewayFixture.ParseJson(await nuevoGen.Content.ReadAsStringAsync())
            .EnumerateArray().Select(e => e.GetProperty("videojuegoId").GetGuid());
        Assert.Contains(id, enB);

        // …y ya no en el viejo
        using var viejoGen = await fix.Http.GetAsync("/api/videojuegos/por-genero/TESTGENA");
        var enA = GatewayFixture.ParseJson(await viejoGen.Content.ReadAsStringAsync())
            .EnumerateArray().Select(e => e.GetProperty("videojuegoId").GetGuid());
        Assert.DoesNotContain(id, enA);

        // Eliminar → 204
        using var eliminar = await fix.AdminDelete($"/api/videojuegos/{id}");
        Assert.Equal(HttpStatusCode.NoContent, eliminar.StatusCode);

        // Ya no existe → 404
        using var verificar = await fix.Http.GetAsync($"/api/videojuegos/{id}");
        Assert.Equal(HttpStatusCode.NotFound, verificar.StatusCode);
    }

    [Fact]
    public async Task Videojuego_ConTorneos_NoSePuedeEliminar_Devuelve409()
    {
        // League of Legends tiene torneos (Worlds, MSI)
        using var r = await fix.AdminDelete($"/api/videojuegos/{fix.LoLId}");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Videojuego_FanNoPuedeEliminar_Devuelve403()
    {
        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");
        using var r = await fix.AuthedDelete($"/api/videojuegos/{fix.LoLId}", fanToken);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    // ─── Usuarios: listar y eliminar ────────────────────────────────────────────

    [Fact]
    public async Task Usuarios_AdminPuedeListar_IncluyeAdmin()
    {
        using var r = await fix.AuthedGet("/api/auth/usuarios", fix.AdminToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var usernames = GatewayFixture.ParseJson(await r.Content.ReadAsStringAsync())
            .EnumerateArray().Select(e => e.GetProperty("username").GetString());
        Assert.Contains("admin", usernames);
    }

    [Fact]
    public async Task Usuarios_FanNoPuedeListar_Devuelve403()
    {
        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");
        using var r = await fix.AuthedGet("/api/auth/usuarios", fanToken);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Usuarios_ListarSinToken_Devuelve401()
    {
        using var r = await fix.Http.GetAsync("/api/auth/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Usuarios_AdminPuedeCrearYEliminar_Devuelve204()
    {
        var username = $"crud_user_{Guid.NewGuid():N}"[..24].ToLowerInvariant();
        using var crear = await fix.AdminPost("/api/auth/register", new
        {
            username,
            password = "FanDemo2024",
            rol = "fan",
            nombreDisplay = "CRUD User"
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);

        using var eliminar = await fix.AdminDelete($"/api/auth/usuarios/{username}");
        Assert.Equal(HttpStatusCode.NoContent, eliminar.StatusCode);

        using var lista = await fix.AuthedGet("/api/auth/usuarios", fix.AdminToken);
        var usernames = GatewayFixture.ParseJson(await lista.Content.ReadAsStringAsync())
            .EnumerateArray().Select(e => e.GetProperty("username").GetString());
        Assert.DoesNotContain(username, usernames);
    }

    [Fact]
    public async Task Usuarios_AdminNoPuedeEliminarseASiMismo_Devuelve409()
    {
        using var r = await fix.AdminDelete("/api/auth/usuarios/admin");
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Usuarios_EliminarInexistente_Devuelve404()
    {
        using var r = await fix.AdminDelete($"/api/auth/usuarios/no_existe_{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Usuarios_FanNoPuedeEliminar_Devuelve403()
    {
        var fanToken = await fix.LoginAsync("fan_demo", "FanDemo2024");
        using var r = await fix.AuthedDelete("/api/auth/usuarios/admin", fanToken);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }
}
