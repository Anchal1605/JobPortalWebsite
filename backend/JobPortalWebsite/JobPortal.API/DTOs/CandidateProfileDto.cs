
namespace JobPortal.API.DTOs;

public class CandidateProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Headline { get; set; }
    public string? Skills { get; set; }
    public int? ExperienceYears { get; set; }
    public string? ResumeUrl { get; set; }
    public string? Location { get; set; }
    public string? AvatarUrl { get; set; }
}
