namespace Travyle.Api.Models;

public class GuideAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingScheduleId { get; set; }
    public Guid GuideUserId { get; set; }
    public string Status { get; set; } = "Assigned"; // Assigned | Active | Completed
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Guide { get; set; }
}
