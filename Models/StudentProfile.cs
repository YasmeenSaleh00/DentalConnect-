using DentBridge.Models.Enums;

namespace DentBridge.Models;

public class StudentProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string UniversityName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public int AcademicYear { get; set; } = 5;
    public string Specialization { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProofDocumentPath { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ApprovedByAdminId { get; set; }

    public double AverageRating { get; set; } = 0;
    public int TotalRatings { get; set; } = 0;
    public int CompletedCases { get; set; } = 0;

    public ICollection<DentalCase> AcceptedCases { get; set; } = new List<DentalCase>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
