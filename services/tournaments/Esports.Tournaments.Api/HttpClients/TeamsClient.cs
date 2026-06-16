using System.Text.Json;
using System.Text.Json.Serialization;

namespace Esports.Tournaments.Api.HttpClients;

public record EquipoDto(Guid EquipoId, string Nombre, string Tag, string Pais, DateTimeOffset FechaCreacion);
public record IntegranteDto(Guid JugadorId, string Nickname, string Nombre, string Pais, string Rol, Guid EquipoId);

public class TeamsClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public TeamsClient(HttpClient http) => _http = http;

    public async Task<EquipoDto?> ObtenerEquipoAsync(Guid equipoId)
    {
        var response = await _http.GetAsync($"/api/equipos/{equipoId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EquipoDto>(json, _json);
    }

    public async Task<IReadOnlyList<Guid>> ObtenerJugadorIdsAsync(Guid equipoId)
    {
        var response = await _http.GetAsync($"/api/equipos/{equipoId}/integrantes");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var integrantes = JsonSerializer.Deserialize<List<IntegranteDto>>(json, _json) ?? [];
        return integrantes.Select(i => i.JugadorId).ToList();
    }
}
