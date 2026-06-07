namespace JobPortal.API.Models;

public class ApplicationStatusHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; } // no FK, service-layer validation
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public int ChangedByUserId { get; set; } // no FK, service-layer validation
}
