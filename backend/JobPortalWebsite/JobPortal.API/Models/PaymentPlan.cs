namespace JobPortal.API.Models;

public class PaymentPlan
{
    public int Id { get; set; }
    public string? Name { get; set; } // Free, Basic, Pro
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int JobPostLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
