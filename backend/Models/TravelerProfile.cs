namespace Travyle.Api.Models;

public class TravelerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Foreign key to User table
    public Guid UserId { get; set; }
    
    // Navigation property
    public User? User { get; set; }

    public string[] PreferredWeather { get; set; } = Array.Empty<string>();
    public string[] PreferredActivities { get; set; } = Array.Empty<string>();
    public string[] PreferredLandscapes { get; set; } = Array.Empty<string>();
    
    public string BudgetRange { get; set; } = string.Empty; // Budget, Mid, Luxury
    
    // Storing past trip history as a JSON string snapshot
    public string TripHistory { get; set; } = string.Empty; 
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
