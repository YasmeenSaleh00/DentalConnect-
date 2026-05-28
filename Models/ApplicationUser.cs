using Microsoft.AspNetCore.Identity;
using DentBridge.Models.Enums;

namespace DentBridge.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string? PhoneNumber2 { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    public StudentProfile? StudentProfile { get; set; }
    public PatientProfile? PatientProfile { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
