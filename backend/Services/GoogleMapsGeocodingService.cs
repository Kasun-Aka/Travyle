using System.Text.Json;

namespace Travyle.Api.Services;

public class GoogleMapsGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GoogleMapsGeocodingService> _logger;

    public GoogleMapsGeocodingService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsGeocodingService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleMaps:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Geocoding failed: Address or API key is empty.");
            return null;
        }

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_apiKey}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString();

            if (status == "OK")
            {
                var results = root.GetProperty("results");
                if (results.GetArrayLength() > 0)
                {
                    var location = results[0].GetProperty("geometry").GetProperty("location");
                    var lat = location.GetProperty("lat").GetDouble();
                    var lng = location.GetProperty("lng").GetDouble();
                    
                    return (lat, lng);
                }
            }
            else
            {
                _logger.LogWarning($"Geocoding API returned status: {status} for address: {address}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while geocoding address: {address}");
        }

        return null;
    }
}
