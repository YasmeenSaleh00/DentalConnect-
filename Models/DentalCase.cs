using DentBridge.Models.Enums;

namespace DentBridge.Models;

public class DentalCase
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TreatmentType TreatmentType { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.Open;
    public string? UrgencyLevel { get; set; }
    public string? PatientNotes { get; set; }
    public bool IsActive { get; set; } = true;

    public int PatientProfileId { get; set; }
    public PatientProfile Patient { get; set; } = null!;

    public int? AssignedStudentId { get; set; }
    public StudentProfile? AssignedStudent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? AppointmentDate { get; set; }

    public string? StudentNotes { get; set; }
    public string? AdminNotes { get; set; }
    public string? Location { get; set; }

    public ICollection<CaseImage> Images { get; set; } = new List<CaseImage>();
    public ICollection<CaseStatusHistory> StatusHistory { get; set; } = new List<CaseStatusHistory>();
    public Review? Review { get; set; }
}
