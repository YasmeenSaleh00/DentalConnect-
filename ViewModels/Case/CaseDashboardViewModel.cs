using DentBridge.Models;
using DentBridge.Models.Enums;

namespace DentBridge.ViewModels.Case;

public class CaseDashboardViewModel
{
    public IEnumerable<DentalCase> MyCases { get; set; } = [];
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int InProgressCases { get; set; }
    public int CompletedCases { get; set; }
    public string PatientName { get; set; } = string.Empty;
}

public class StudentDashboardViewModel
{
    public IEnumerable<DentalCase> AvailableCases { get; set; } = [];
    public IEnumerable<DentalCase> MyCases { get; set; } = [];
    public int TotalAccepted { get; set; }
    public int TotalCompleted { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public AccountStatus AccountStatus { get; set; }

    // filter state
    public TreatmentType? FilterType { get; set; }
    public string? SearchTerm { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TotalStudents { get; set; }
    public int PendingStudents { get; set; }
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int InProgressCases { get; set; }
    public int CompletedCases { get; set; }
    public IEnumerable<DentalCase> RecentCases { get; set; } = [];
    public IEnumerable<Models.StudentProfile> PendingApprovals { get; set; } = [];
    public Dictionary<string, int> CasesByType { get; set; } = new();
    public Dictionary<string, int> CasesLast7Days { get; set; } = new();
    public int PendingTestimonials { get; set; }
    public int ApprovedTestimonials { get; set; }
}
