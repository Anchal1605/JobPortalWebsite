namespace JobPortal.API.DTOs;

public class ProfileMeDto
{
    public int RoleId { get; set; }
    public bool IsProfileComplete { get; set; }
    public int CompletionPercent { get; set; }
    public List<string> MissingFields { get; set; } = new();
    public CandidateProfileDto? Candidate { get; set; }
    public EmployerProfileDto? Employer { get; set; }
}
