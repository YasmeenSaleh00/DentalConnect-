using DentBridge.Models.Enums;

namespace DentBridge.Models;

public class CaseStatusHistory
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public DentalCase Case { get; set; } = null!;
    public CaseStatus OldStatus { get; set; }
    public CaseStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
