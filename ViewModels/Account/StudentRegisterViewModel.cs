using System.ComponentModel.DataAnnotations;

namespace DentBridge.ViewModels.Account;

public class StudentRegisterViewModel : RegisterViewModel
{
    [Required]
    [Display(Name = "University Name")]
    public string UniversityName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Specialization")]
    public string Specialization { get; set; } = string.Empty;

    [Display(Name = "Short Bio")]
    [MaxLength(500)]
    public string? Bio { get; set; }

    [Required]
    [Display(Name = "Proof Document (PDF/Image)")]
    public IFormFile ProofDocument { get; set; } = null!;

    public StudentRegisterViewModel() => Role = "Student";
}
