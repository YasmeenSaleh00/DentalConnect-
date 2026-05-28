using System.ComponentModel.DataAnnotations;

namespace DentBridge.ViewModels.Case;

public class SubmitReviewViewModel
{
    public int CaseId { get; set; }
    public string CaseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Display(Name = "Was the student professional?")]
    public bool IsProfessional { get; set; } = true;

    [Display(Name = "Was the treatment timely?")]
    public bool IsTimely { get; set; } = true;

    [Display(Name = "Would you recommend this student?")]
    public bool IsRecommended { get; set; } = true;
}
