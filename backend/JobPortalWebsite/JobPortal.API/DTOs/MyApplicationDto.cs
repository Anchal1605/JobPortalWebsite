namespace JobPortal.API.DTOs
{
    public class MyApplicationDto
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string? JobTitle { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public string? ResumeUrl { get; set; }
    }
}
