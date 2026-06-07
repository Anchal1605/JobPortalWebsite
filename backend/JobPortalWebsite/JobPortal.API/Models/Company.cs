namespace JobPortal.API.Models;

public class Company
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
