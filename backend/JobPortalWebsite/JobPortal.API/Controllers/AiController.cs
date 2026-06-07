using System.Security.Claims;
using System.Text.Json;
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
public class AiController : ControllerBase
{
    private readonly ILlmMatchService _llmService;
    private readonly AppDbContext _context;
    private readonly ResumeTextExtractor _resumeExtractor;
    private readonly FileStorageService _fileStorage;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiController(
        ILlmMatchService llmService,
        AppDbContext context,
        ResumeTextExtractor resumeExtractor,
        FileStorageService fileStorage)
    {
        _llmService = llmService;
        _context = context;
        _resumeExtractor = resumeExtractor;
        _fileStorage = fileStorage;
    }

    [Authorize(Roles = "3")]
    [HttpPost("match/{jobId:int}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> MatchJobToCandidate(int jobId, [FromForm] AiMatchRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiResponse<string>
            {
                Success = false,
                Message = "Unauthorized access",
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

        var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        var hasUploadedResume = request.ResumeFile != null && request.ResumeFile.Length > 0;
        if (hasUploadedResume)
        {
            var validationError = _fileStorage.ValidateResume(request.ResumeFile);
            if (validationError != null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = validationError,
                    Data = null
                });
            }
        }

        if (candidate == null)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Complete your candidate profile before using AI match.",
                Data = null
            });
        }

        var hasProfileResume = !string.IsNullOrWhiteSpace(candidate.ResumeUrl);
        var hasProfileContext = !string.IsNullOrWhiteSpace(candidate.Skills)
            || !string.IsNullOrWhiteSpace(candidate.Headline);

        if (!hasUploadedResume && !hasProfileResume && !hasProfileContext)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Add skills, a headline, or a résumé before using AI match.",
                Data = null
            });
        }

        string? resumeText;
        CandidateJobMatchCache? cached = null;

        if (hasUploadedResume)
        {
            resumeText = _resumeExtractor.ExtractFromFormFile(request.ResumeFile);
            if (string.IsNullOrWhiteSpace(resumeText) && !hasProfileContext)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Could not read text from the uploaded PDF. Try a different file.",
                    Data = null
                });
            }
        }
        else
        {
            cached = await _context.CandidateJobMatchCaches
                .FirstOrDefaultAsync(c => c.UserId == userId && c.JobId == jobId);

            if (cached != null && cached.ProfileUpdatedAt == candidate.UpdatedAt)
            {
                return Ok(new ApiResponse<MatchScoreDto>
                {
                    Success = true,
                    Message = "Match score calculated successfully (from cache)",
                    Data = ToMatchScoreDto(cached),
                });
            }

            resumeText = _resumeExtractor.ExtractFromPublicUrl(candidate.ResumeUrl);
        }

        var matchScore = await _llmService.GetMatchScoreAsync(job, candidate, resumeText);
        if (matchScore == null)
        {
            return StatusCode(503, new ApiResponse<string>
            {
                Success = false,
                Message = "AI is unavailable. Check your Llm:ApiKey or try again later.",
                Data = null
            });
        }

        if (!hasUploadedResume)
        {
            await UpsertCacheAsync(userId, jobId, candidate.UpdatedAt, matchScore, cached);
        }

        return Ok(new ApiResponse<MatchScoreDto>
        {
            Success = true,
            Message = "Match score calculated successfully",
            Data = matchScore
        });
    }

    private async Task UpsertCacheAsync(
        int userId,
        int jobId,
        DateTime profileUpdatedAt,
        MatchScoreDto matchScore,
        CandidateJobMatchCache? existing)
    {
        var entry = existing ?? new CandidateJobMatchCache { UserId = userId, JobId = jobId };
        if (existing == null)
        {
            _context.CandidateJobMatchCaches.Add(entry);
        }

        entry.ProfileUpdatedAt = profileUpdatedAt;
        entry.ComputedAt = DateTime.UtcNow;
        entry.Score = matchScore.Score;
        entry.StrengthsJson = JsonSerializer.Serialize(matchScore.Strengths, _jsonOptions);
        entry.GapsJson = JsonSerializer.Serialize(matchScore.Gaps, _jsonOptions);
        entry.Summary = matchScore.Summary;
        entry.Tip = matchScore.Tip;
        await _context.SaveChangesAsync();
    }

    private static MatchScoreDto ToMatchScoreDto(CandidateJobMatchCache cached)
    {
        return new MatchScoreDto
        {
            Score = cached.Score,
            Strengths = JsonSerializer.Deserialize<List<string>>(cached.StrengthsJson, _jsonOptions) ?? [],
            Gaps = JsonSerializer.Deserialize<List<string>>(cached.GapsJson, _jsonOptions) ?? [],
            Summary = cached.Summary ?? string.Empty,
            Tip = cached.Tip ?? string.Empty
        };
    }
}
