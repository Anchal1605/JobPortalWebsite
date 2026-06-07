namespace JobPortal.API.Models;

public class EmployerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; } // no FK, service-layer validation
    public int CompanyId { get; set; } // no FK, service-layer validation
    public string? Designation { get; set; }
    public string? ContactNumber { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
