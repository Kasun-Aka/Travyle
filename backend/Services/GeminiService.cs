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
        _apiKey = config["Gemini:ApiKey"]
            ?? config["GEMINI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
            ?? "";
    }

    private async Task<HttpResponseMessage> SendGeminiRequestAsync(object payload)
    {
        var primaryModel = "gemini-2.5-flash";
        var fallbackModel = "gemini-1.5-flash";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{primaryModel}:generateContent?key={_apiKey}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Add("x-goog-api-key", _apiKey);
        }

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var fallbackUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{fallbackModel}:generateContent?key={_apiKey}";
            using var fallbackReq = new HttpRequestMessage(HttpMethod.Post, fallbackUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(_apiKey))
            {
                fallbackReq.Headers.Add("x-goog-api-key", _apiKey);
            }
            return await _httpClient.SendAsync(fallbackReq);
        }

        return response;
    }

    public virtual async Task<string> GetRecommendationAsync(string preferences, string budget, string tripHistory, string destinationsJson)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY")
        {
            throw new InvalidOperationException("Gemini API key is missing or not configured.");
        }

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

        var response = await SendGeminiRequestAsync(requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Error ({response.StatusCode}): {errorJson}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(responseJson);
        string? text = null;
        if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var candidate = candidates[0];
            if (candidate.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
            {
                text = parts[0].GetProperty("text").GetString();
            }
        }

        if (text != null && text.StartsWith("```json"))
        {
            text = text.Replace("```json", "").Replace("```", "").Trim();
        }

        return text ?? "{}";
    }

    public virtual async Task<string> GetItineraryAsync(string destinationName, string region, string preferences, string budget, string tripHistory)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY")
        {
            throw new InvalidOperationException("Gemini API key is missing or not configured.");
        }

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

        var response = await SendGeminiRequestAsync(payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode} - {error}");
        }

        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        string? textResponse = null;
        if (jsonDoc != null && jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var candidate = candidates[0];
            if (candidate.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
            {
                textResponse = parts[0].GetProperty("text").GetString();
            }
        }

        textResponse = textResponse?.Trim() ?? "[]";
        if (textResponse.StartsWith("```json"))
        {
            textResponse = textResponse.Substring(7);
            if (textResponse.EndsWith("```"))
                textResponse = textResponse.Substring(0, textResponse.Length - 3);
        }

        return textResponse.Trim();
    }
}
