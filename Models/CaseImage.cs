namespace DentBridge.Models;

public class CaseImage
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public DentalCase Case { get; set; } = null!;
    public string ImagePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; } = false;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
