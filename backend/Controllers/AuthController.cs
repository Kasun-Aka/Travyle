using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;
using Travyle.Api.Services.Auth;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly TravyleDbContext _db;
    private readonly IFirebaseIdentityService _identityService;

    public AuthController(TravyleDbContext db, IFirebaseIdentityService identityService)
    {
        _db = db;
        _identityService = identityService;
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
    public async Task<IActionResult> SyncUser([FromBody] SyncRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.FirebaseUid) || string.IsNullOrEmpty(req.Email))
            return BadRequest("FirebaseUid and Email are required");

        var identity = await _identityService.VerifyFirebaseTokenAsync(Request, ct);
        if (identity == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });
        if (!string.Equals(identity.Uid, req.FirebaseUid, StringComparison.Ordinal) ||
            !string.Equals(identity.Email, req.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUid == req.FirebaseUid || u.Email == req.Email);

        if (user == null)
        {
            user = new User
            {
                FirebaseUid = req.FirebaseUid,
                Email = req.Email,
                FullName = req.FullName,
                Role = req.Role switch
                {
                    "Local Guide" => "Local Guide",
                    "Tour Operator" => "Tour Operator",
                    _ => "Traveler"
                }
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(user.FirebaseUid)) // In case existing DB users don't have FirebaseUid yet
        {
            user.FirebaseUid = req.FirebaseUid;
            await _db.SaveChangesAsync();
        }
        else if (!string.Equals(user.FirebaseUid, req.FirebaseUid, StringComparison.Ordinal))
        {
            return Conflict(new { error = "This email is already linked to another Firebase account." });
        }

        return Ok(user);
    }

    // GET /api/auth/user
    [HttpGet("user")]
    public async Task<IActionResult> GetUserByEmail([FromQuery] string? email, CancellationToken ct)
    {
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });

        if (!string.IsNullOrWhiteSpace(email) &&
            !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        return Ok(user);
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    // PUT /api/auth/user
    [HttpPut("user")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });
        if (!string.Equals(req.Email, user.Email, StringComparison.OrdinalIgnoreCase)) return Forbid();

        if (!string.IsNullOrEmpty(req.FullName))
        {
            user.FullName = req.FullName;
        }

        await _db.SaveChangesAsync();

        return Ok(user);
    }
}
