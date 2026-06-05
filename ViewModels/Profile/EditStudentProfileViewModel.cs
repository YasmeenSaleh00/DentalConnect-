using System.ComponentModel.DataAnnotations;

namespace DentBridge.ViewModels.Profile;

public class EditStudentProfileViewModel
{
    [Required, MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Phone]
    [Display(Name = "Secondary Phone")]
    public string? PhoneNumber2 { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }

    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(500)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? Avatar { get; set; }

    public string? CurrentAvatarPath { get; set; }

    // Read-only — shown in the form but not posted back
    public string UniversityName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
}
