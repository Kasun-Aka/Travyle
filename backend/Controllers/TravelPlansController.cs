using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Travyle.Api.Data;
using Travyle.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/travel-plans")]
public class TravelPlansController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public TravelPlansController(TravyleDbContext db)
    {
        _db = db;
    }

    public class CreateTravelPlanDto
    {
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = "My Personalized Trip";
        public Guid DestinationId { get; set; }
        public int DurationDays { get; set; } = 3;
        public decimal EstimatedBudget { get; set; } = 0.0m;
        public JsonElement AiItineraryData { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> SaveTravelPlan([FromBody] CreateTravelPlanDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email)) return BadRequest("Email is required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return NotFound("User not found");

        var plan = new PersonalizedItinerary
        {
            TravelerId = user.Id,
            Title = dto.Title,
            DestinationIds = JsonSerializer.Serialize(new[] { dto.DestinationId }),
            DurationDays = dto.DurationDays,
            EstimatedBudget = dto.EstimatedBudget,
            AiItineraryData = JsonSerializer.Serialize(dto.AiItineraryData),
            Status = "Finalized"
        };

        _db.PersonalizedItineraries.Add(plan);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTravelPlan), new { id = plan.Id }, plan);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTravelPlan(Guid id)
    {
        var plan = await _db.PersonalizedItineraries
            .Include(p => p.Traveler)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var plan = await _db.PersonalizedItineraries
            .Include(p => p.Traveler)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound("Plan not found");

        var user = plan.Traveler;
        var destinationIds = JsonSerializer.Deserialize<List<Guid>>(plan.DestinationIds) ?? new List<Guid>();
        var firstDestId = destinationIds.FirstOrDefault();
        var destination = await _db.Destinations.FindAsync(firstDestId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text($"Travyle - {plan.Title}")
                    .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                {
                    x.Spacing(10);
                    x.Item().Text($"Traveler: {user?.FullName ?? "Unknown"}").FontSize(16).SemiBold();
                    x.Item().Text($"Duration: {plan.DurationDays} Days");
                    x.Item().Text($"Destination: {destination?.Name ?? "Unknown"}");

                    x.Item().PaddingTop(15).Text("Your AI Itinerary").SemiBold().FontSize(18).Underline();

                    try
                    {
                        using var doc = JsonDocument.Parse(plan.AiItineraryData);
                        var root = doc.RootElement;
                        var daysList = root.ValueKind == JsonValueKind.Array ? root : 
                                       (root.TryGetProperty("days", out var d) ? d : root);

                        if (daysList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var day in daysList.EnumerateArray())
                            {
                                var dayNum = day.TryGetProperty("day", out var dNum) ? dNum.ToString() : "";
                                var title = day.TryGetProperty("title", out var t) ? t.GetString() : "";
                                var desc = day.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";

                                x.Item().PaddingTop(10).Text($"Day {dayNum}: {title}").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken1);
                                x.Item().Text(desc);
                            }
                        }
                        else
                        {
                            x.Item().Text("No detailed itinerary available.");
                        }
                    }
                    catch
                    {
                        x.Item().Text("Failed to parse itinerary details.");
                    }

                    x.Item().PaddingTop(25).Text("Have a wonderful journey!").Italic().FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Itinerary_{plan.Id}.pdf");
    }
}
