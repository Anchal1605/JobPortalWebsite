using JobPortal.API.Data;
using JobPortal.API.DTOs;
using JobPortal.API.Models;
using JobPortal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorage;

        public ApplicationsController(AppDbContext context, FileStorageService fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        [Authorize(Roles = "3")]
        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ApplyForJob([FromForm] int jobId, [FromForm] IFormFile? resumeFile, [FromForm] string? resumeUrl)
        {
            try
            {
                var candidateUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(candidateUserIdClaim))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Unauthorized access",
                        Data = null
                    });
                }
                if (!int.TryParse(candidateUserIdClaim, out int candidateUserId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid user identity in token",
                        Data = null
                    });
                }
                if (jobId <= 0)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "JobId is required",
                        Data = null
                    });
                }

                string? resolvedResumeUrl = null;
                if (resumeFile != null && resumeFile.Length > 0)
                {
                    var validationError = _fileStorage.ValidateResume(resumeFile);
                    if (validationError != null)
                    {
                        return BadRequest(new ApiResponse<string>
                        {
                            Success = false,
                            Message = validationError,
                            Data = null
                        });
                    }

                    resolvedResumeUrl = await _fileStorage.SaveFileAsync(resumeFile, "resumes", $"user-{candidateUserId}");
                }
                else if (!string.IsNullOrWhiteSpace(resumeUrl))
                {
                    resolvedResumeUrl = resumeUrl.Trim();
                }

                if (string.IsNullOrEmpty(resolvedResumeUrl))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Upload a PDF résumé or use the one saved on your profile",
                        Data = null
                    });
                }

                var alreadyApplied = await _context.Applications.AnyAsync(x => x.JobId == jobId && x.UserId == candidateUserId);
                if (alreadyApplied)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "You have already applied for this job. Please wait for the employer to review your application.",
                        Data = null
                    });
                }
                var application = new Application
                {
                    JobId = jobId,
                    UserId = candidateUserId,
                    Status = "Applied",
                    ResumeUrl = resolvedResumeUrl
                };
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Application submitted successfully",
                    Data = application.Id.ToString()
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while applying for job",
                    Data = null
                });
            }
        }

        [Authorize(Roles = "2")]
        [HttpGet("forJob/{jobId}")]
        public async Task<IActionResult> GetJobApplicants(int jobId)
        {
            try
            {
                var employerUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employerUserIdClaim))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Unauthorized access",
                        Data = null
                    });
                }
                if (!int.TryParse(employerUserIdClaim, out int employerUserId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid user identity in token",
                        Data = null
                    });
                }
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Job not found",
                        Data = null
                    });
                }
                if (job.PostedBy != employerUserId)
                {
                    return StatusCode(403, new ApiResponse<string>
                    {
                        Success = false,
                        Message = "You are not authorized to view applications for this job",
                        Data = null
                    });
                }
                var applications = await (
                    from a in _context.Applications
                    join u in _context.Users on a.UserId equals u.Id
                    join cp in _context.CandidateProfiles on u.Id equals cp.UserId into cpJoin
                    from cp in cpJoin.DefaultIfEmpty()
                    where a.JobId == jobId
                    orderby a.Id descending
                    select new ApplicantDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        CandidateName = u.Name,
                        Headline = cp != null ? cp.Headline : null,
                        Status = a.Status,
                        ResumeUrl = a.ResumeUrl
                    }).ToListAsync();

                return Ok(new ApiResponse<List<ApplicantDto>>
                {
                    Success = true,
                    Message = "Applications fetched successfully",
                    Data = applications
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while fetching applications for job",
                    Data = null
                });
            }
        }

        [Authorize(Roles = "3")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyApplications()
        {
            try
            {
                var candidateUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(candidateUserIdClaim))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Unauthorized access",
                        Data = null
                    });
                }
                if (!int.TryParse(candidateUserIdClaim, out int candidateUserId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid user identity in token",
                        Data = null
                    });
                }

                var list = await (
                    from a in _context.Applications
                    join j in _context.Jobs on a.JobId equals j.Id
                    where a.UserId == candidateUserId
                    orderby a.Id descending
                    select new MyApplicationDto
                    {
                        Id = a.Id,
                        JobId = j.Id,
                        JobTitle = j.Title,
                        Company = j.Company,
                        Location = j.Location,
                        Status = a.Status,
                        ResumeUrl = a.ResumeUrl
                    }).ToListAsync();

                return Ok(new ApiResponse<List<MyApplicationDto>>
                {
                    Success = true,
                    Message = "Applications fetched successfully",
                    Data = list
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while fetching your applications",
                    Data = null
                });
            }
        }

        [Authorize(Roles = "2")]
        [HttpPatch("{applicationId}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, UpdateApplicationStatusRequest request)
        {
            try
            {
                var employerUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(employerUserIdClaim)|| 
                !int.TryParse(employerUserIdClaim, out int employerUserId))
                {
                    return Unauthorized(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Unauthorized access",
                        Data = null
                    });
                }
                if(request == null || string.IsNullOrEmpty(request.Status))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Status is required",
                        Data = null
                    });
                }
                var allowedStatuses = new List<string>
                 { 
                    ApplicationStatuses.Applied,
                     ApplicationStatuses.UnderReview,
                      ApplicationStatuses.Shortlisted,
                       ApplicationStatuses.Interview,
                        ApplicationStatuses.Rejected,
                         ApplicationStatuses.Accepted 
                         };
                         var status = request.Status.Trim();
                if (!allowedStatuses.Contains(status))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid status",
                        Data = null
                    });
                }
                var application = await _context.Applications.FindAsync(applicationId);
                if (application == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Application not found",
                        Data = null
                    });
                }
                var job = await _context.Jobs.FindAsync(application.JobId);
                if (job == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Job not found",
                        Data = null
                    });
                }
                if (job.PostedBy != employerUserId) 
                {
                    return StatusCode(403, new ApiResponse<string> { Success = false, Message = "You are not authorized to update the status of this application", Data = null });
                }
                application.Status = status;
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<string> { Success = true, Message = "Application status updated successfully", Data = null });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = "Something went wrong while updating application status", Data = null });
            }
        }
    }
}
