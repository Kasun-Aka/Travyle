using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public AuthController(TravyleDbContext db)
    {
        _db = db;
    }

    public class SyncRequest
    {
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Traveler";
    }

    // POST /api/auth/sync
    [HttpPost("sync")]
    public async Task<IActionResult> SyncUser([FromBody] SyncRequest req)
    {
        if (string.IsNullOrEmpty(req.FirebaseUid) || string.IsNullOrEmpty(req.Email))
            return BadRequest("FirebaseUid and Email are required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUid == req.FirebaseUid || u.Email == req.Email);

        if (user == null)
        {
            user = new User
            {
                FirebaseUid = req.FirebaseUid,
                Email = req.Email,
                FullName = req.FullName,
                Role = req.Role
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(user.FirebaseUid)) // In case existing DB users don't have FirebaseUid yet
        {
            user.FirebaseUid = req.FirebaseUid;
            await _db.SaveChangesAsync();
        }

        return Ok(user);
    }

    // GET /api/auth/user
    [HttpGet("user")]
    public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = "Traveler",
                Role = "Traveler"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return Ok(user);
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UpdatePreferencesRequest
    {
        public string Email { get; set; } = string.Empty;
        public string[] Preferences { get; set; } = Array.Empty<string>();
    }

    // PUT /api/auth/user/preferences
    [HttpPut("user/preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest req)
    {
        if (string.IsNullOrEmpty(req.Email))
            return BadRequest("Email is required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
        {
            user = new User
            {
                Email = req.Email,
                FullName = "Traveler",
                Role = "Traveler"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var profile = await _db.TravelerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new TravelerProfile { UserId = user.Id };
            _db.TravelerProfiles.Add(profile);
        }

        profile.PreferredActivities = req.Preferences;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(profile);
    }

    // PUT /api/auth/user
    [HttpPut("user")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest req)
    {
        if (string.IsNullOrEmpty(req.Email))
            return BadRequest("Email is required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            return NotFound("User not found");

        if (!string.IsNullOrEmpty(req.FullName))
        {
            user.FullName = req.FullName;
        }

        if (!string.IsNullOrEmpty(req.Role))
        {
            user.Role = req.Role;
        }

        await _db.SaveChangesAsync();

        return Ok(user);
    }

    // POST /api/auth/avatar
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] string email, IFormFile file)
    {
        if (string.IsNullOrEmpty(email)) return BadRequest("Email is required");
        if (file == null || file.Length == 0) return BadRequest("File is required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("User not found");

        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var avatarsPath = Path.Combine(webRootPath, "avatars");

        if (!Directory.Exists(avatarsPath))
        {
            Directory.CreateDirectory(avatarsPath);
        }

        var fileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(avatarsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return the full URL for the Flutter app
        // NOTE: In a real production app, the base URL would be read from configuration
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host.Value}";
        var avatarUrl = $"{baseUrl}/avatars/{fileName}";

        return Ok(new { url = avatarUrl });
    }
}
