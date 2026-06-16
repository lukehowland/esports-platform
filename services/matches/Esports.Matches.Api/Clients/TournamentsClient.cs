using System.Net.Http.Json;

namespace Esports.Matches.Api.Clients;

public record TorneoResumenDto(Guid TorneoId, Guid OrganizadorId);

public class TournamentsClient
{
    private readonly HttpClient _http;
    public TournamentsClient(HttpClient http) => _http = http;

    public async Task<TorneoResumenDto?> ObtenerTorneoAsync(Guid torneoId)
    {
        try
        {
            return await _http.GetFromJsonAsync<TorneoResumenDto>($"/api/torneos/{torneoId}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
