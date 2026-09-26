using System.Text.Json;

namespace Travyle.Api.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Nominatim requires a valid User-Agent header
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Travyle-UniversityProject/1.0");
    }

    public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        try
        {
            var coordinates = await FetchFromNominatimAsync(address);
            
            // Fallback: If "Name, Region" fails (because OSM is very strict), try just the "Name"
            if (coordinates == null && address.Contains(','))
            {
                var fallbackAddress = address.Split(',')[0].Trim();
                _logger.LogInformation($"Geocoding fallback: trying '{fallbackAddress}' instead of '{address}'");
                coordinates = await FetchFromNominatimAsync(fallbackAddress);
            }

            if (coordinates != null) return coordinates;
            else _logger.LogWarning($"Nominatim API returned no results for address: {address}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while geocoding address with Nominatim: {address}");
        }

        return null;
    }
    private async Task<(double, double)?> FetchFromNominatimAsync(string query)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&limit=1";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            var firstResult = root[0];
            var latString = firstResult.GetProperty("lat").GetString();
            var lngString = firstResult.GetProperty("lon").GetString();
            
            if (double.TryParse(latString, out var lat) && double.TryParse(lngString, out var lng))
            {
                return (lat, lng);
            }
        }
        return null;
    }
}
