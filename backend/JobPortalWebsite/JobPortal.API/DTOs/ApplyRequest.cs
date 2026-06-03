using Microsoft.AspNetCore.Http;
namespace JobPortal.API.DTOs
{
    public class ApplyRequest
    {
        public int JobId { get; set; }
        public string? ResumeUrl { get; set; }
        public IFormFile? ResumeFile { get; set; }
    }
}