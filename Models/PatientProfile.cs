namespace DentBridge.Models;

public class PatientProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? BloodType { get; set; }

    public ICollection<DentalCase> Cases { get; set; } = new List<DentalCase>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
