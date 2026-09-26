with open("backend/Services/NominatimGeocodingService.cs", "r", encoding="utf-8") as f:
    text = f.read()

old_logic = """        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";
            
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
            else
            {
                _logger.LogWarning($"Nominatim API returned no results for address: {address}");
            }
        }"""

new_logic = """        try
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
        }"""

text = text.replace(old_logic, new_logic)

helper = """    private async Task<(double, double)?> FetchFromNominatimAsync(string query)
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
"""

# insert helper before the last closing brace
last_brace = text.rfind("}")
text = text[:last_brace] + helper + "}\n"

with open("backend/Services/NominatimGeocodingService.cs", "w", encoding="utf-8") as f:
    f.write(text)

print("Updated Nominatim Service with fallback logic")
