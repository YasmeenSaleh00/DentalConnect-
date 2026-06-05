using System.ComponentModel.DataAnnotations;

namespace DentBridge.ViewModels.Profile;

public class EditPatientProfileViewModel
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

    [Display(Name = "Medical History")]
    public string? MedicalHistory { get; set; }

    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }

    [Display(Name = "Emergency Contact Name")]
    public string? EmergencyContactName { get; set; }

    [Phone]
    [Display(Name = "Emergency Contact Phone")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "Insurance Provider")]
    public string? InsuranceProvider { get; set; }

    [Display(Name = "Blood Type")]
    public string? BloodType { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? Avatar { get; set; }

    public string? CurrentAvatarPath { get; set; }
}
