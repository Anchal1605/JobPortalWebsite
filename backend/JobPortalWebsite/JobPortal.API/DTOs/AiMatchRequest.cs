using Microsoft.AspNetCore.Http;

namespace JobPortal.API.DTOs;

public class AiMatchRequest
{
    public IFormFile? ResumeFile { get; set; }
}
