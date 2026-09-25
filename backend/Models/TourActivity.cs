namespace Travyle.Api.Models;

public class TourActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingScheduleId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
}
