using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public HealthController(TravyleDbContext db)
    {
        _db = db;
    }

    // GET /api/health
    [HttpGet]
    public async Task<IActionResult> Check()
    {
        try
        {
            // Runs a trivial query: SELECT 1 — fastest possible DB round-trip
            await _db.Database.ExecuteSqlRawAsync("SELECT 1");

            return Ok(new
            {
                status = "healthy",
                database = "connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "unreachable",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
