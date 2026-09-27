namespace Travyle.Api.Models;

public class RouteLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingScheduleId { get; set; }
    public Guid RecordedBy { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Recorder { get; set; }
}
