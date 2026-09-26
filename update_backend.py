with open("backend/Controllers/AuthController.cs", "r", encoding="utf-8") as f:
    text = f.read()

new_endpoint = """    // GET /api/auth/user/preferences
    [HttpGet("user/preferences")]
    public async Task<IActionResult> GetPreferences([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email)) return BadRequest("Email is required");

        var user = await _db.Users
            .Include(u => u.TravelerProfile)
            .FirstOrDefaultAsync(u => u.Email == email);
            
        if (user == null) return NotFound("User not found");

        var prefs = user.TravelerProfile?.PreferredActivities ?? Array.Empty<string>();
        return Ok(prefs);
    }

    // PUT /api/auth/user/preferences"""

text = text.replace('    // PUT /api/auth/user/preferences', new_endpoint)

with open("backend/Controllers/AuthController.cs", "w", encoding="utf-8") as f:
    f.write(text)

print("Added GetPreferences endpoint")
