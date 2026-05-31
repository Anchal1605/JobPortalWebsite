namespace JobPortal.API.DTOs
{
    public class ApplicantDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? CandidateName { get; set; }
        public string? Headline { get; set; }
        public string? Status { get; set; }
        public string? ResumeUrl { get; set; }
    }
}
