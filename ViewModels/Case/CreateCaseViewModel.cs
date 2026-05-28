using System.ComponentModel.DataAnnotations;
using DentBridge.Models.Enums;

namespace DentBridge.ViewModels.Case;

public class CreateCaseViewModel
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Treatment Type")]
    public TreatmentType TreatmentType { get; set; }

    [Display(Name = "Urgency Level")]
    public string UrgencyLevel { get; set; } = "Normal";

    [Display(Name = "Preferred Location / Clinic Area")]
    public string? Location { get; set; }

    [Display(Name = "Additional Notes")]
    [MaxLength(1000)]
    public string? PatientNotes { get; set; }

    [Display(Name = "Preferred Appointment Date")]
    [DataType(DataType.Date)]
    public DateTime? AppointmentDate { get; set; }

    [Display(Name = "Case Images (Optional)")]
    public List<IFormFile>? Images { get; set; }
}
