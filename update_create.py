import re

with open("backend/Controllers/DestinationsController.cs", "r", encoding="utf-8") as f:
    text = f.read()

old_create = """    public async Task<ActionResult<DestinationDto>> CreateDestination([FromBody] CreateDestinationDto dto)
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
        };"""

new_create = """    public async Task<ActionResult<DestinationDto>> CreateDestination([FromBody] CreateDestinationDto dto)
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
        };"""

text = text.replace(old_create, new_create)

old_update = """    public async Task<ActionResult<DestinationDto>> UpdateDestination(Guid id, [FromBody] CreateDestinationDto dto)
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
        if (dto.Longitude.HasValue) destination.Longitude = dto.Longitude.Value;"""

new_update = """    public async Task<ActionResult<DestinationDto>> UpdateDestination(Guid id, [FromBody] CreateDestinationDto dto)
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
        destination.Longitude = newLng;"""

text = text.replace(old_update, new_update)

with open("backend/Controllers/DestinationsController.cs", "w", encoding="utf-8") as f:
    f.write(text)

print("Updated DestinationsController")
