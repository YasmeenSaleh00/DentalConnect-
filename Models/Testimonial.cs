using DentBridge.Models.Enums;

namespace DentBridge.Models;

public class Testimonial
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty; 

    public string? AvatarPath { get; set; }

    public int Rating { get; set; } 

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;

    public string? AdminNotes { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedByAdminId { get; set; }
}
