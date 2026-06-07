namespace JobPortal.API.Models;

public class Application
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public int UserId { get; set; }
    public string? Status { get; set; }
    public string? ResumeUrl { get; set; }
}
