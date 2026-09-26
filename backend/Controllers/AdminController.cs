using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public AdminController(TravyleDbContext db)
    {
        _db = db;
    }

    // GET /api/admin/staff
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        var staff = await _db.Users
            .Where(u => u.Role == "Guide" || u.Role == "Local Guide" || u.Role == "Tour Operator" || u.Role == "Operator")
            .Select(u => new
            {
                Id = u.Id,
                FirebaseUid = u.FirebaseUid,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(staff);
    }

    [HttpGet("live-operations")]
    public IActionResult GetLiveOperations()
    {
        var data = new
        {
            stats = new
            {
                toursInProgress = 4,
                flagged = 2,
                guidesOnDuty = 4,
                guidesAvailable = 2,
                activeAlerts = 2,
                avgScheduleDrift = "+11 min"
            },
            activeTours = new[]
            {
                new { id = "TOUR-5510", name = "Ella Highlands & Tea Trails", guide = "Ravi Perera", progress = "3/6", progressPercent = 50, status = "Disruption", lastPing = "40 s ago" },
                new { id = "TOUR-5508", name = "Galle Fort Coastal Loop", guide = "Nadia Silva", progress = "5/7", progressPercent = 71, status = "On schedule", lastPing = "1 min ago" },
                new { id = "TOUR-5505", name = "Yala Wildlife Expedition", guide = "Ishara Bandara", progress = "1/4", progressPercent = 25, status = "On schedule", lastPing = "6 min ago" },
                new { id = "TOUR-5502", name = "Sigiriya & Ancient Cities", guide = "Tharindu Jay", progress = "4/8", progressPercent = 50, status = "Disruption", lastPing = "12 min ago" }
            },
            alerts = new[]
            {
                new { id = "ALT-841", tourId = "TOUR-5510", title = "Heavy rain warning around Nine Arches Bridge until 15:00", source = "OpenWeatherMap", time = "5 min ago", severity = "High", proposal = "Reorder stops 4 & 6, move viewpoint to late afternoon (+18 min total)", status = "Pending" },
                new { id = "ALT-912", tourId = "TOUR-5502", title = "Road closure on A9 near Dambulla junction", source = "Traffic", time = "22 min ago", severity = "Medium", proposal = "Detour via B152, swap lunch stop earlier (+8 min total)", status = "Pending" },
                new { id = "ALT-908", tourId = "TOUR-5508", title = "High UV index, afternoon walking segment flagged", source = "OpenWeatherMap", time = "1 hr ago", severity = "Low", proposal = "Add 20 min shade break after stop 4", status = "Approved" }
            }
        };
        return Ok(data);
    }

    [HttpGet("guide-matrix")]
    public async Task<IActionResult> GetGuideMatrix()
    {
        var guides = await _db.Users
            .Where(u => u.Role == "Guide" || u.Role == "Local Guide")
            .Select(u => new
            {
                Id = u.Id,
                Name = u.FullName,
                Meta = "Verified • " + u.Email
            })
            .ToListAsync();

        var data = new
        {
            guides = guides.Select(g => new
            {
                id = g.Id,
                name = g.Name,
                meta = g.Meta,
                schedule = new[] { "-", "TOUR-5510", "-", "SLOT-8841", "SLOT-8841", "-" }
            }),
            roster = guides.Select(g => new
            {
                name = g.Name,
                meta = g.Meta,
                languages = new[] { "EN", "SI" },
                verification = "Verified",
                load = "3 tours",
                status = "Available"
            })
        };

        return Ok(data);
    }
}
