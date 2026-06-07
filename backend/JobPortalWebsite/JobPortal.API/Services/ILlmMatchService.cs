using JobPortal.API.DTOs;
using JobPortal.API.Models;

namespace JobPortal.API.Services;

public interface ILlmMatchService
{
    Task<MatchScoreDto?> GetMatchScoreAsync(Job job, CandidateProfile profile, string? resumeText = null, CancellationToken ct = default);
}
