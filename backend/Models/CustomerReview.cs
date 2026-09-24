using System.ComponentModel.DataAnnotations;

namespace Travyle.Api.Models;

public class CustomerReview
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public Guid TourId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;

    public bool IsVerified { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
