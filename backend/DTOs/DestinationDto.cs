namespace Travyle.Api.DTOs;

public class DestinationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
}

public class CreateDestinationDto
{
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string ImageUrl { get; set; } = string.Empty;
    
    // We expect the frontend to pass the address/region, 
    // and the backend will geocode it if lat/lon aren't provided.
    // For now, we'll allow passing them directly as well.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
