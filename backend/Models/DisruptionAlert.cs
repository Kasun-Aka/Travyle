namespace Travyle.Api.Models;

public class DisruptionAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingScheduleId { get; set; }
    public string Type { get; set; } = string.Empty; // Weather | Traffic
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
