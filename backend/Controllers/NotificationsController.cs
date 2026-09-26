using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public NotificationsController(TravyleDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null) return NotFound("User not found");

        var notif = new TravelerNotification
        {
            UserId = user.Id,
            DestinationId = req.DestinationId,
            Pitch = req.Pitch
        };

        _db.TravelerNotifications.Add(notif);
        await _db.SaveChangesAsync();

        return Ok(notif);
    }

    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminNotifications()
    {
        var notifs = await _db.TravelerNotifications
            .Include(n => n.User)
            .Include(n => n.Destination)
            .OrderByDescending(n => n.SentAt)
            .Take(50)
            .Select(n => new
            {
                n.Id,
                n.Pitch,
                n.IsRead,
                n.SentAt,
                UserEmail = n.User!.Email,
                UserName = n.User!.FullName,
                DestinationName = n.Destination!.Name,
                DestinationRegion = n.Destination!.Region
            })
            .ToListAsync();
        
        return Ok(notifs);
    }

    [HttpGet("traveler/{email}")]
    public async Task<IActionResult> GetTravelerNotifications(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        var notifs = await _db.TravelerNotifications
            .Include(n => n.Destination)
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new
            {
                n.Id,
                n.Pitch,
                n.IsRead,
                n.SentAt,
                Destination = n.Destination
            })
            .ToListAsync();

        return Ok(notifs);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notif = await _db.TravelerNotifications.FindAsync(id);
        if (notif == null) return NotFound();

        notif.IsRead = true;
        await _db.SaveChangesAsync();

        return Ok();
    }
}

public class SendNotificationDto
{
    public string Email { get; set; } = string.Empty;
    public Guid DestinationId { get; set; }
    public string Pitch { get; set; } = string.Empty;
}
