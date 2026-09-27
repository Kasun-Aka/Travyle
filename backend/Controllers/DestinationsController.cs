using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DestinationsController : ControllerBase
{
    private readonly TravyleDbContext _db;
    private readonly Services.IGeocodingService _geocodingService;

    public DestinationsController(TravyleDbContext db, Services.IGeocodingService geocodingService)
    {
        _db = db;
        _geocodingService = geocodingService;
    }

    // GET /api/destinations?search=ella&region=highlands&tags=hiking,beach&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<object>> GetDestinations(
        [FromQuery] string? search,
        [FromQuery] string? region,
        [FromQuery] string? tags,
        [FromQuery] string? sortBy = "createdat",
        [FromQuery] string? sortDir = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Destinations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(lower) ||
                d.Region.ToLower().Contains(lower) ||
                d.Description.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(region))
        {
            query = query.Where(d => d.Region.ToLower().Contains(region.ToLower()));
        }

        // Tag filter: comma-separated list of tags; destination must contain ALL specified tags
        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(t => t.Trim().ToLower())
                              .ToArray();
            foreach (var tag in tagList)
            {
                query = query.Where(d => d.Tags.Any(t => t.ToLower() == tag));
            }
        }

        // Sorting
        var isAsc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLower() switch
        {
            "name" => isAsc ? query.OrderBy(d => d.Name) : query.OrderByDescending(d => d.Name),
            "region" => isAsc ? query.OrderBy(d => d.Region) : query.OrderByDescending(d => d.Region),
            "rating" => isAsc ? query.OrderBy(d => d.AverageRating) : query.OrderByDescending(d => d.AverageRating),
            _ => isAsc ? query.OrderBy(d => d.CreatedAt) : query.OrderByDescending(d => d.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DestinationDto
            {
                Id = d.Id,
                Name = d.Name,
                Region = d.Region,
                Description = d.Description,
                Tags = d.Tags,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                ImageUrl = d.ImageUrl,
                AverageRating = d.AverageRating
            })
            .ToListAsync();

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            items
        });
    }

    // GET /api/destinations/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DestinationDto>> GetDestination(Guid id)
    {
        var destination = await _db.Destinations.FindAsync(id);
        if (destination is null) return NotFound();

        return Ok(new DestinationDto
        {
            Id = destination.Id,
            Name = destination.Name,
            Region = destination.Region,
            Description = destination.Description,
            Tags = destination.Tags,
            Latitude = destination.Latitude,
            Longitude = destination.Longitude,
            ImageUrl = destination.ImageUrl,
            AverageRating = destination.AverageRating
        });
    }

    // POST /api/destinations
    [HttpPost]
    public async Task<ActionResult<DestinationDto>> CreateDestination([FromBody] CreateDestinationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var lat = dto.Latitude ?? 0.0;
        var lng = dto.Longitude ?? 0.0;

        if (lat == 0.0 && lng == 0.0)
        {
            var coords = await _geocodingService.GetCoordinatesAsync($"{dto.Name}, {dto.Region}");
            if (coords != null)
            {
                lat = coords.Value.Latitude;
                lng = coords.Value.Longitude;
            }
        }

        var destination = new Destination
        {
            Name = dto.Name,
            Region = dto.Region,
            Description = dto.Description,
            Tags = dto.Tags,
            ImageUrl = dto.ImageUrl,
            Latitude = lat,
            Longitude = lng
        };

        _db.Destinations.Add(destination);
        await _db.SaveChangesAsync();

        var result = new DestinationDto
        {
            Id = destination.Id,
            Name = destination.Name,
            Region = destination.Region,
            Description = destination.Description,
            Tags = destination.Tags,
            Latitude = destination.Latitude,
            Longitude = destination.Longitude,
            ImageUrl = destination.ImageUrl,
            AverageRating = destination.AverageRating
        };

        return CreatedAtAction(nameof(GetDestination), new { id = destination.Id }, result);
    }

    // PUT /api/destinations/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DestinationDto>> UpdateDestination(Guid id, [FromBody] CreateDestinationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var destination = await _db.Destinations.FindAsync(id);
        if (destination is null) return NotFound();

        destination.Name = dto.Name;
        destination.Region = dto.Region;
        destination.Description = dto.Description;
        destination.Tags = dto.Tags;
        destination.ImageUrl = dto.ImageUrl;
        
        var newLat = dto.Latitude ?? destination.Latitude;
        var newLng = dto.Longitude ?? destination.Longitude;

        // Re-geocode if name/region changed and coordinates are 0 (or manually reset to 0 to trigger geocode)
        if (newLat == 0.0 && newLng == 0.0)
        {
            var coords = await _geocodingService.GetCoordinatesAsync($"{dto.Name}, {dto.Region}");
            if (coords != null)
            {
                newLat = coords.Value.Latitude;
                newLng = coords.Value.Longitude;
            }
        }

        destination.Latitude = newLat;
        destination.Longitude = newLng;

        await _db.SaveChangesAsync();

        return Ok(new DestinationDto
        {
            Id = destination.Id,
            Name = destination.Name,
            Region = destination.Region,
            Description = destination.Description,
            Tags = destination.Tags,
            Latitude = destination.Latitude,
            Longitude = destination.Longitude,
            ImageUrl = destination.ImageUrl,
            AverageRating = destination.AverageRating
        });
    }

    // DELETE /api/destinations/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDestination(Guid id)
    {
        var destination = await _db.Destinations.FindAsync(id);
        if (destination is null) return NotFound();

        _db.Destinations.Remove(destination);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    public class GeneratePdfRequest
    {
        public string Email { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
    }

    [HttpPost("generate-itinerary-pdf")]
    public async Task<IActionResult> GenerateItineraryPdf([FromBody] GeneratePdfRequest req)
    {
        if (string.IsNullOrEmpty(req.Email)) return BadRequest("Email is required");

        QuestPDF.Settings.License = LicenseType.Community;

        var user = await _db.Users
            .Include(u => u.TravelerProfile)
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null) return NotFound("User not found");

        var preferences = user.TravelerProfile?.PreferredActivities ?? Array.Empty<string>();
        var prefString = preferences.Length > 0 ? string.Join(", ", preferences) : "General Travel";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text("Travyle - Custom Itinerary Pass")
                    .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                {
                    x.Spacing(10);

                    x.Item().Text($"Traveler: {user.FullName}").FontSize(16).SemiBold();
                    x.Item().Text($"Email: {user.Email}");
                    
                    x.Item().PaddingTop(15).Text("Destination Details").SemiBold().FontSize(14).Underline();
                    x.Item().Text($"Destination: {(string.IsNullOrEmpty(req.DestinationName) ? "Personalized AI Match" : req.DestinationName)}");
                    
                    if (!string.IsNullOrEmpty(req.DateRange))
                    {
                        x.Item().Text($"Dates: {req.DateRange}");
                    }

                    x.Item().PaddingTop(15).Text("Your Preferences").SemiBold().FontSize(14).Underline();
                    x.Item().Text(prefString);

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
        return File(pdfBytes, "application/pdf", "TravelItinerary.pdf");
    }
}
