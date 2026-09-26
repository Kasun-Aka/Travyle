using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Travyle.Api.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key is missing");
    }

    public async Task<string> GetRecommendationAsync(string preferences, string budget, string tripHistory, string destinationsJson)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={_apiKey}";
        
        var prompt = $@"
        You are an expert AI travel agent for 'Travyle'.
        User Preferences: {preferences}
        Available Destinations: {destinationsJson}
        
        Based on the user preferences, select the single best destination from the available list.
        Provide a short, exciting paragraph (about 2-3 sentences) explaining to the user exactly why it's the perfect match for their specific style.
        Format the response strictly as JSON with no markdown wrapping:
        {{
            ""destinationId"": ""UUID of the chosen destination"",
            ""reasoning"": ""Your exciting explanation""
        }}
        ";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Error ({response.StatusCode}): {errorJson}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString();

        if (text != null && text.StartsWith("```json"))
        {
            text = text.Replace("```json", "").Replace("```", "").Trim();
        }

        return text ?? "{}";
    }
    public async Task<string> GetItineraryAsync(string destinationName, string region, string preferences, string budget, string tripHistory)
    {
        var prompt = $@"
You are a master travel concierge AI.

TRAVELER PROFILE:
- Style Preferences: {preferences}
- Budget Range: {budget}
- Past Trip History: {tripHistory}

DESTINATION: {destinationName}, {region}

Task:
Generate a highly personalized 3-day itinerary for this traveler at this destination.
Ensure the activities match their Style Preferences and Budget.

Return ONLY a JSON array of 3 objects (one for each day), with no markdown formatting or extra text. Each object must have this exact format:
{{
  ""day"": 1,
  ""title"": ""Brief Day Title"",
  ""description"": ""A highly engaging, personalized 2-sentence description of the day's activities.""
}}
";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 1024,
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"?key={_apiKey}", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode} - {error}");
        }

        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var candidates = jsonDoc.RootElement.GetProperty("candidates");
        var textResponse = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

        textResponse = textResponse.Trim();
        if (textResponse.StartsWith("```json"))
        {
            textResponse = textResponse.Substring(7);
            if (textResponse.EndsWith("```"))
                textResponse = textResponse.Substring(0, textResponse.Length - 3);
        }

        return textResponse.Trim();
    }
}
