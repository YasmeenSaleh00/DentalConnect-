using System.ComponentModel.DataAnnotations;

namespace DentBridge.ViewModels.Testimonial;

public class SubmitTestimonialViewModel
{
    [Required, MaxLength(150)]
    [Display(Name = "Your Name")]
    public string Name { get; set; } = string.Empty;

    [Required, Range(1, 5)]
    [Display(Name = "Rating")]
    public int Rating { get; set; } = 5;

    [Required, MaxLength(1000), MinLength(20)]
    [Display(Name = "Your Message")]
    public string Message { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty; // set server-side
}
