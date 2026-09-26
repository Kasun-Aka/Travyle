using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Travyle.Api.Data;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly TravyleDbContext _db;
    private readonly GeminiService _geminiService;

    public AgentController(TravyleDbContext db, GeminiService geminiService)
    {
        _db = db;
        _geminiService = geminiService;
    }

    public class RecommendRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    [HttpPost("recommend")]
    public async Task<IActionResult> GetRecommendation([FromBody] RecommendRequest req)
    {
        if (string.IsNullOrEmpty(req.Email)) return BadRequest("Email is required");

        var user = await _db.Users
            .Include(u => u.TravelerProfile)
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null) return NotFound("User not found");

        var preferences = user.TravelerProfile?.PreferredActivities ?? Array.Empty<string>();
        var prefString = preferences.Length > 0 ? string.Join(", ", preferences) : "General Travel";
        var budget = user.TravelerProfile?.BudgetRange ?? "Flexible";
        var tripHistory = user.TravelerProfile?.TripHistory ?? "First time traveler";

        // Get up to 20 destinations to pass to the AI
        var destinations = await _db.Destinations
            .Select(d => new { d.Id, d.Name, d.Region, d.Description })
            .Take(20)
            .ToListAsync();

        if (!destinations.Any()) return NotFound("No destinations available in the database.");

        var destinationsJson = JsonSerializer.Serialize(destinations);

        try
        {
            var geminiMatchJson = await _geminiService.GetRecommendationAsync(prefString, budget, tripHistory, destinationsJson);
            
            var matchData = JsonSerializer.Deserialize<JsonElement>(geminiMatchJson);
            var destIdStr = matchData.GetProperty("destinationId").GetString();
            var reasoning = matchData.GetProperty("reasoning").GetString();

            if (Guid.TryParse(destIdStr, out var destId))
            {
                var matchedDest = await _db.Destinations.FindAsync(destId);
                if (matchedDest != null)
                {
                    return Ok(new
                    {
                        destination = matchedDest,
                        reasoning = reasoning
                    });
                }
            }
            return BadRequest("The AI agent failed to match a valid destination ID.");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("503") || ex.Message.Contains("ServiceUnavailable") || ex.Message.Contains("high demand"))
            {
                return StatusCode(503, "Google's AI servers are currently experiencing high demand. Please try again in a few moments.");
            }
            return StatusCode(500, $"AI matching failed: {ex.Message}");
        }
    }
}
