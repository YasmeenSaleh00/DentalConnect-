namespace DentBridge.Models;

public class Review
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public DentalCase Case { get; set; } = null!;

    public int PatientProfileId { get; set; }
    public PatientProfile Patient { get; set; } = null!;

    public int StudentProfileId { get; set; }
    public StudentProfile Student { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsProfessional { get; set; } = true;
    public bool IsTimely { get; set; } = true;
    public bool IsRecommended { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
