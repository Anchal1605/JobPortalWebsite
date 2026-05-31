namespace JobPortal.API.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; } 
        public string? Headline { get; set; }
        public string? Skills { get; set; }
        public int? ExperienceYears { get; set; }
        public string? ResumeUrl { get; set; }
        public string? Location { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}