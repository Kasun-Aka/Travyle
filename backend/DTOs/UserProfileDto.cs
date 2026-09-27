namespace Travyle.Api.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string[] PreferredWeather { get; set; } = Array.Empty<string>();
    public string[] PreferredActivities { get; set; } = Array.Empty<string>();
    public string[] PreferredLandscapes { get; set; } = Array.Empty<string>();
    public string BudgetRange { get; set; } = string.Empty;
    public string TripHistory { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string[] PreferredWeather { get; set; } = Array.Empty<string>();
    public string[] PreferredActivities { get; set; } = Array.Empty<string>();
    public string[] PreferredLandscapes { get; set; } = Array.Empty<string>();
    public string BudgetRange { get; set; } = string.Empty;
}
