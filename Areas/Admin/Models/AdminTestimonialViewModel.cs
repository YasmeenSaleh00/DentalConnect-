using DentBridge.Models;
using DentBridge.Models.Enums;

namespace DentBridge.Areas.Admin.Models;

public class AdminTestimonialsViewModel
{
    public IEnumerable<Testimonial> Pending { get; set; } = [];
    public IEnumerable<Testimonial> Approved { get; set; } = [];
    public IEnumerable<Testimonial> Rejected { get; set; } = [];
    public int TotalCount { get; set; }
    public TestimonialStatus? Filter { get; set; }
    public IEnumerable<Testimonial> Filtered { get; set; } = [];
}
