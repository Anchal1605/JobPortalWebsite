using JobPortal.API.Data;
using JobPortal.API.DTOs;
using JobPortal.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetJobs( 
            [FromQuery] string? search, 
            [FromQuery] string? location, 
            [FromQuery] string? type)
        {
            try
            {
                var query = _context.Jobs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        (x.Title != null && x.Title.ToLower().Contains(term)) ||
                        (x.Description != null && x.Description.ToLower().Contains(term)) ||
                        (x.Company != null && x.Company.ToLower().Contains(term)));
                }
                if (!string.IsNullOrWhiteSpace(location))
                {
                    var loc = location.Trim().ToLower();
                    query = query.Where(x =>
                        x.Location != null && x.Location.ToLower().Contains(loc));
                }
                if (!string.IsNullOrWhiteSpace(type))
                {
                    var jobType = type.Trim().ToLower();
                    query = query.Where(x =>
                        x.Type != null && x.Type.ToLower().Contains(jobType));
                }
                var jobs = await query.OrderByDescending(x => x.Id).ToListAsync();
                return Ok(new ApiResponse<List<Job>>
                {
                    Success = true,
                    Message = "Jobs fetched successfully",
                    Data = jobs
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<List<Job>>
                {
                    Success = false,
                    Message = "Something went wrong while fetching jobs",
                    Data = null
                });
            }
        }
        
        [Authorize(Roles = "2")]
        [HttpGet("employer-jobs")]
        public async Task<IActionResult> GetJobsByEmployer()
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
                var jobs = await _context.Jobs.Where(x => x.PostedBy == employerUserId).OrderByDescending(x => x.Id).ToListAsync();
                return Ok(new ApiResponse<List<Job>>
                {
                    Success = true,
                    Message = "Jobs fetched successfully",
                    Data = jobs
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while fetching jobs",
                    Data = null
                });
            }
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobById(int jobId)
        {
            try
            {
                var existingJob = await _context.Jobs.FindAsync(jobId);
                if (existingJob == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Job not found",
                        Data = null
                    });
                }
                return Ok(new ApiResponse<Job>
                {
                    Success = true,
                    Message = "Job fetched successfully",
                    Data = existingJob
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while fetching job",
                    Data = null
                });
            }
        }

        //this API CAN ONLY BE ACCESSED BY LOGGED IN USERS , ONLY IF THEIR JWT CONTAINS ROLE CLAIM VALUE 2
        [Authorize(Roles = "2")]
        [HttpPost]
        public async Task<IActionResult> CreateJob(Job job)
        {
            try
            {
                //Take the employer user id from the token
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
                if (String.IsNullOrEmpty(job.Title) || String.IsNullOrEmpty(job.Description))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Title and Description are required",
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

                job.PostedBy = employerUserId;
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<Job>
                {
                    Success = true,
                    Message = "Job created successfully",
                    Data = job
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while creating job",
                    Data = null
                });
            }
        }


        [Authorize(Roles = "2")]
        [HttpPut("{jobId}")]
        public async Task<IActionResult> UpdateJob(int jobId, Job job)
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

                var existingJob = await _context.Jobs.FindAsync(jobId);
                if (existingJob == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Job not found",
                        Data = null
                    });
                }

                if (existingJob.PostedBy != employerUserId)
                {
                    return StatusCode(403, new ApiResponse<string>
                    {
                        Success = false,
                        Message = "You can only update your own jobs",
                        Data = null
                    });
                }

                if (String.IsNullOrEmpty(job.Title) || String.IsNullOrEmpty(job.Description))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Title and Description are required",
                        Data = null
                    });
                }
                existingJob.Title = job.Title;
                existingJob.Description = job.Description;
                existingJob.Location = job.Location;
                existingJob.Company = job.Company;
                existingJob.Salary = job.Salary;
                existingJob.Type = job.Type;
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<Job>
                {
                    Success = true,
                    Message = "Job updated successfully",
                    Data = existingJob
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while updating job",
                    Data = null
                });
            }
        }

        [Authorize(Roles = "2")]
        [HttpDelete("{jobId}")]
        public async Task<IActionResult> DeleteJob(int jobId)
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

                var existingJob = await _context.Jobs.FindAsync(jobId);
                if (existingJob == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Job not found",
                        Data = null
                    });
                }
                if (existingJob.PostedBy != employerUserId)
                {
                    return StatusCode(403, new ApiResponse<string>
                    {
                        Success = false,
                        Message = "You can only delete your own jobs",
                        Data = null
                    });
                }
                _context.Jobs.Remove(existingJob);
                await _context.SaveChangesAsync();
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Job deleted successfully",
                    Data = null
                });
            }
            catch
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Something went wrong while deleting job",
                    Data = null
                });
            }
        }

    }
}