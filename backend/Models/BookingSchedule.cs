namespace Travyle.Api.Models;

public class BookingSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DestinationId { get; set; }
    public string DestinationTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string GuideName { get; set; } = string.Empty;
    public decimal PricePerPerson { get; set; }
    public int MaxCapacityPerSlot { get; set; }
    public double Rating { get; set; }
    public int ReviewsCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string SlotOverrides { get; set; } = "[]";

    // Navigation
    public ICollection<ScheduleAvailableDate> AvailableDates { get; set; } = new List<ScheduleAvailableDate>();
    public ICollection<ScheduleTimeSlot> TimeSlots { get; set; } = new List<ScheduleTimeSlot>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class ScheduleAvailableDate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingScheduleId { get; set; }
    public DateTime Date { get; set; }

    public BookingSchedule BookingSchedule { get; set; } = null!;
}

public class ScheduleTimeSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingScheduleId { get; set; }
    public string SlotLabel { get; set; } = string.Empty; // e.g. "06:30 AM"

    public BookingSchedule BookingSchedule { get; set; } = null!;
}
