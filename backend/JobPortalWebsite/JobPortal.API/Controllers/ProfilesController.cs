using System.Security.Claims;
using JobPortal.API.Data;
using JobPortal.API.DTOs;
using JobPortal.API.Models;
using JobPortal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FileStorageService _fileStorage;

    private const int RecruiterRoleId = 2;
    private const int CandidateRoleId = 3;

    public ProfilesController(AppDbContext context, FileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            if (user.RoleId == CandidateRoleId)
            {
                var candidate = await BuildCandidateDtoAsync(user);
                var completion = ComputeCandidateCompletion(user, candidate);
                return Ok(new ApiResponse<ProfileMeDto>
                {
                    Success = true,
                    Message = "Profile loaded",
                    Data = new ProfileMeDto
                    {
                        RoleId = user.RoleId,
                        IsProfileComplete = completion.IsComplete,
                        CompletionPercent = completion.Percent,
                        MissingFields = completion.Missing,
                        Candidate = candidate,
                        Employer = null
                    }
                });
            }

            if (user.RoleId == RecruiterRoleId)
            {
                var employer = await BuildEmployerDtoAsync(user);
                var completion = ComputeEmployerCompletion(user, employer);
                return Ok(new ApiResponse<ProfileMeDto>
                {
                    Success = true,
                    Message = "Profile loaded",
                    Data = new ProfileMeDto
                    {
                        RoleId = user.RoleId,
                        IsProfileComplete = completion.IsComplete,
                        CompletionPercent = completion.Percent,
                        MissingFields = completion.Missing,
                        Candidate = null,
                        Employer = employer
                    }
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Profile is not available for this role",
                Data = null
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while loading profile",
                Data = null
            });
        }
    }

    [Authorize(Roles = "3")]
    [HttpPost("me/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.RoleId != CandidateRoleId)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            var validationError = _fileStorage.ValidateImage(file);
            if (validationError != null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = validationError,
                    Data = null
                });
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new CandidateProfile { UserId = user.Id };
                _context.CandidateProfiles.Add(profile);
            }

            _fileStorage.TryDeleteUploadedFile(profile.AvatarUrl);
            var publicUrl = await _fileStorage.SaveImageAsync(file, "avatars", $"avatar-{user.Id}");
            profile.AvatarUrl = publicUrl;
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<UploadFileResultDto>
            {
                Success = true,
                Message = "Photo uploaded",
                Data = new UploadFileResultDto { Url = publicUrl }
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while uploading photo",
                Data = null
            });
        }
    }

    [Authorize(Roles = "3")]
    [HttpPost("me/resume")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.RoleId != CandidateRoleId)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            var validationError = _fileStorage.ValidateResume(file);
            if (validationError != null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = validationError,
                    Data = null
                });
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new CandidateProfile { UserId = user.Id };
                _context.CandidateProfiles.Add(profile);
            }

            _fileStorage.TryDeleteUploadedFile(profile.ResumeUrl);
            var publicUrl = await _fileStorage.SaveFileAsync(file, "resumes", $"resume-{user.Id}");
            profile.ResumeUrl = publicUrl;
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<UploadFileResultDto>
            {
                Success = true,
                Message = "Résumé uploaded",
                Data = new UploadFileResultDto { Url = publicUrl }
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while uploading résumé",
                Data = null
            });
        }
    }

    [Authorize(Roles = "2")]
    [HttpPost("me/company-logo")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadCompanyLogo(IFormFile file)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.RoleId != RecruiterRoleId)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            var validationError = _fileStorage.ValidateImage(file);
            if (validationError != null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = validationError,
                    Data = null
                });
            }

            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            Company? company;
            if (employerProfile == null)
            {
                company = new Company { CreatedAt = DateTime.UtcNow };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                employerProfile = new EmployerProfile
                {
                    UserId = user.Id,
                    CompanyId = company.Id
                };
                _context.EmployerProfiles.Add(employerProfile);
            }
            else
            {
                company = await _context.Companies.FindAsync(employerProfile.CompanyId);
                if (company == null)
                {
                    company = new Company { CreatedAt = DateTime.UtcNow };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                    employerProfile.CompanyId = company.Id;
                }
            }

            _fileStorage.TryDeleteUploadedFile(company!.LogoUrl);
            var publicUrl = await _fileStorage.SaveImageAsync(file, "logos", $"logo-{user.Id}");
            company.LogoUrl = publicUrl;
            employerProfile!.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<UploadFileResultDto>
            {
                Success = true,
                Message = "Logo uploaded",
                Data = new UploadFileResultDto { Url = publicUrl }
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while uploading logo",
                Data = null
            });
        }
    }

    [Authorize(Roles = "3")]
    [HttpPut("me/candidate")]
    public async Task<IActionResult> UpdateCandidateProfile(UpdateCandidateProfileRequest request)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.RoleId != CandidateRoleId)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name.Trim();
            }

            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new CandidateProfile { UserId = user.Id };
                _context.CandidateProfiles.Add(profile);
            }

            profile.Headline = request.Headline?.Trim();
            profile.Skills = request.Skills?.Trim();
            profile.ExperienceYears = request.ExperienceYears;
            if (!string.IsNullOrWhiteSpace(request.ResumeUrl))
            {
                profile.ResumeUrl = request.ResumeUrl.Trim();
            }
            profile.Location = request.Location?.Trim();
            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                profile.AvatarUrl = request.AvatarUrl?.Trim();
            }
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<CandidateProfileDto>
            {
                Success = true,
                Message = "Profile saved",
                Data = await BuildCandidateDtoAsync(user)
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while saving profile",
                Data = null
            });
        }
    }

    [Authorize(Roles = "2")]
    [HttpPut("me/employer")]
    public async Task<IActionResult> UpdateEmployerProfile(UpdateEmployerProfileRequest request)
    {
        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null || user.RoleId != RecruiterRoleId)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Unauthorized access",
                    Data = null
                });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name.Trim();
            }

            var employerProfile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            Company? company = null;
            if (employerProfile == null)
            {
                company = new Company
                {
                    Name = request.CompanyName?.Trim(),
                    Website = request.CompanyWebsite?.Trim(),
                    Location = request.CompanyLocation?.Trim(),
                    Description = request.CompanyDescription?.Trim(),
                    LogoUrl = request.CompanyLogoUrl?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                employerProfile = new EmployerProfile
                {
                    UserId = user.Id,
                    CompanyId = company.Id
                };
                _context.EmployerProfiles.Add(employerProfile);
            }
            else
            {
                company = await _context.Companies.FindAsync(employerProfile.CompanyId);
                if (company == null)
                {
                    company = new Company { CreatedAt = DateTime.UtcNow };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                    employerProfile.CompanyId = company.Id;
                }
            }

            employerProfile.Designation = request.Designation?.Trim();
            employerProfile.ContactNumber = request.ContactNumber?.Trim();
            employerProfile.UpdatedAt = DateTime.UtcNow;

            if (company != null)
            {
                company.Name = request.CompanyName?.Trim();
                company.Website = request.CompanyWebsite?.Trim();
                company.Location = request.CompanyLocation?.Trim();
                company.Description = request.CompanyDescription?.Trim();
                if (!string.IsNullOrWhiteSpace(request.CompanyLogoUrl))
                {
                    company.LogoUrl = request.CompanyLogoUrl?.Trim();
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<EmployerProfileDto>
            {
                Success = true,
                Message = "Profile saved",
                Data = await BuildEmployerDtoAsync(user)
            });
        }
        catch
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "Something went wrong while saving profile",
                Data = null
            });
        }
    }

    private static (bool IsComplete, int Percent, List<string> Missing) ComputeCandidateCompletion(
        User user,
        CandidateProfileDto dto)
    {
        var checks = new (string Label, bool Ok)[]
        {
            ("Full name", !string.IsNullOrWhiteSpace(dto.Name ?? user.Name)),
            ("Headline", !string.IsNullOrWhiteSpace(dto.Headline)),
            ("Skills", !string.IsNullOrWhiteSpace(dto.Skills)),
            ("Location", !string.IsNullOrWhiteSpace(dto.Location)),
            ("Résumé link", !string.IsNullOrWhiteSpace(dto.ResumeUrl)),
        };

        return BuildCompletion(checks);
    }

    private static (bool IsComplete, int Percent, List<string> Missing) ComputeEmployerCompletion(
        User user,
        EmployerProfileDto dto)
    {
        var checks = new (string Label, bool Ok)[]
        {
            ("Name", !string.IsNullOrWhiteSpace(dto.Name ?? user.Name)),
            ("Job title", !string.IsNullOrWhiteSpace(dto.Designation)),
            ("Company name", !string.IsNullOrWhiteSpace(dto.Company?.Name)),
            ("Company location", !string.IsNullOrWhiteSpace(dto.Company?.Location)),
        };

        return BuildCompletion(checks);
    }

    private static (bool IsComplete, int Percent, List<string> Missing) BuildCompletion(
        (string Label, bool Ok)[] checks)
    {
        var missing = checks.Where(c => !c.Ok).Select(c => c.Label).ToList();
        var filled = checks.Count(c => c.Ok);
        var percent = checks.Length == 0
            ? 100
            : (int)Math.Round(100.0 * filled / checks.Length);
        return (missing.Count == 0, percent, missing);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return null;
        }

        return await _context.Users.FindAsync(userId);
    }

    private async Task<CandidateProfileDto> BuildCandidateDtoAsync(User user)
    {
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (profile == null)
        {
            return new CandidateProfileDto
            {
                Id = 0,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email
            };
        }

        return new CandidateProfileDto
        {
            Id = profile.Id,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Headline = profile.Headline,
            Skills = profile.Skills,
            ExperienceYears = profile.ExperienceYears,
            ResumeUrl = profile.ResumeUrl,
            Location = profile.Location,
            AvatarUrl = profile.AvatarUrl
        };
    }

    private async Task<EmployerProfileDto> BuildEmployerDtoAsync(User user)
    {
        var profile = await _context.EmployerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (profile == null)
        {
            return new EmployerProfileDto
            {
                Id = 0,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Company = null
            };
        }

        var company = await _context.Companies.FindAsync(profile.CompanyId);

        return new EmployerProfileDto
        {
            Id = profile.Id,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Designation = profile.Designation,
            ContactNumber = profile.ContactNumber,
            Company = company == null
                ? null
                : new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Website = company.Website,
                    Location = company.Location,
                    Description = company.Description,
                    LogoUrl = company.LogoUrl
                }
        };
    }
}
