using System;
using System.ComponentModel.DataAnnotations;

namespace Travyle.Api.Models;

public class TravelerNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public Guid DestinationId { get; set; }
    public Destination? Destination { get; set; }

    [Required]
    public string Pitch { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
