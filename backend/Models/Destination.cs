namespace Travyle.Api.Models;

public class Destination
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty; // Country / province
    public string Description { get; set; } = string.Empty;
    
    public string[] Tags { get; set; } = Array.Empty<string>(); // Searchable tags
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public string ImageUrl { get; set; } = string.Empty; // Supabase Storage URL
    
    public decimal AverageRating { get; set; } = 0.0m; // Computed from reviews (Student 4)
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
