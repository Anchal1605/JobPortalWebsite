namespace JobPortal.API.DTOs
{
    public class ApplyRequest
    {
        public int JobId { get; set; }
        public string? ResumeUrl { get; set; }
    }
}