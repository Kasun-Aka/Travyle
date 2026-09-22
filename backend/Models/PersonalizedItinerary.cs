namespace Travyle.Api.Models;

public class PersonalizedItinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Foreign key to User table (Traveler)
    public Guid TravelerId { get; set; }
    
    // Navigation property
    public User? Traveler { get; set; }

    public string Title { get; set; } = string.Empty; // e.g., "My Sri Lanka Trip"
    
    // Storing lists as JSON string snapshots for simplicity in the relational schema
    public string DestinationIds { get; set; } = "[]"; 
    
    public string PreferencesSnapshot { get; set; } = "{}";
    
    public int DurationDays { get; set; }
    
    public decimal EstimatedBudget { get; set; }
    
    public string PdfUrl { get; set; } = string.Empty; // Supabase Storage URL after generation
    
    public string Status { get; set; } = "Draft"; // Draft, Finalized
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
