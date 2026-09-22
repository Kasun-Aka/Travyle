using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DestinationsController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public DestinationsController(TravyleDbContext db)
    {
        _db = db;
    }

    // GET /api/destinations?search=ella&region=highlands&tags=hiking,beach&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<object>> GetDestinations(
        [FromQuery] string? search,
        [FromQuery] string? region,
        [FromQuery] string? tags,
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

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
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

        var destination = new Destination
        {
            Name = dto.Name,
            Region = dto.Region,
            Description = dto.Description,
            Tags = dto.Tags,
            ImageUrl = dto.ImageUrl,
            Latitude = dto.Latitude ?? 0.0,
            Longitude = dto.Longitude ?? 0.0
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
        if (dto.Latitude.HasValue) destination.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue) destination.Longitude = dto.Longitude.Value;

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
}
