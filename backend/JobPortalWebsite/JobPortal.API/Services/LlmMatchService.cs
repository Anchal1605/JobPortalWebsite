using System.Text;
using System.Text.Json;
using JobPortal.API.DTOs;
using JobPortal.API.Models;
using JobPortal.API.Options;
using Microsoft.Extensions.Options;

namespace JobPortal.API.Services;

public class LlmMatchService : ILlmMatchService
{
    private readonly HttpClient _http;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmMatchService> _logger;

    public LlmMatchService(HttpClient http, IOptions<LlmOptions> options, ILogger<LlmMatchService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MatchScoreDto?> GetMatchScoreAsync(
  Job job,
  CandidateProfile profile,
  string? resumeText = null,
  CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return null;
        }

        var jobDescription = job.Description ?? "";
        jobDescription = StripHtml(jobDescription);
        if (jobDescription.Length > 2000)
        {
            jobDescription = jobDescription[..2000] + "...";
        }

        var hasResume = !string.IsNullOrWhiteSpace(resumeText);

        var systemPrompt = hasResume
            ? """
        You are a career coach. Compare a job posting to a candidate r�sum� (contact details already removed).
        Weight the r�sum� heavily; use profile fields only as extra context.
        Do not guess or infer email, phone, or address.
        Respond with ONLY valid JSON, no markdown, no extra text.
        Keep each strength and gap under 12 words. Max 3 items each.
        Schema:
        {"score":0-100,"strengths":["string"],"gaps":["string"],"summary":"one short sentence","tip":"one actionable sentence"}
        Be fair. Weak r�sum� fit = lower score.
        """
            : """
        You are a career coach. Compare a job posting to a candidate profile.
        Respond with ONLY valid JSON, no markdown, no extra text.
        Keep each strength and gap under 12 words. Max 3 items each.
        Schema:
        {"score":0-100,"strengths":["string"],"gaps":["string"],"summary":"one short sentence","tip":"one actionable sentence"}
        Be fair. Sparse profile = lower score.
        """;

        var userPrompt = hasResume
            ? $"""
        JOB:
        Title: {job.Title}
        Description: {jobDescription}
        Location: {job.Location}
        Type: {job.Type}

        CANDIDATE R�SUM� (redacted):
        {resumeText}

        PROFILE (supplementary):
        Headline: {profile.Headline}
        Skills: {profile.Skills}
        Experience years: {profile.ExperienceYears}
        Location: {profile.Location}
        """
            : $"""
        JOB:
        Title: {job.Title}
        Description: {jobDescription}
        Location: {job.Location}
        Type: {job.Type}

        CANDIDATE:
        Headline: {profile.Headline}
        Skills: {profile.Skills}
        Experience years: {profile.ExperienceYears}
        Location: {profile.Location}
        """;
        var combinedPrompt = systemPrompt + "\n\n" + userPrompt;

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = combinedPrompt } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 512,
                responseMimeType = "application/json",
                thinkingConfig = new { thinkingBudget = 0 }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(req, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API failed {StatusCode}: {Body}", response.StatusCode, responseText);
            return null;
        }

        using var doc = JsonDocument.Parse(responseText);
        var candidate = doc.RootElement.GetProperty("candidates")[0];
        var finishReason = candidate.TryGetProperty("finishReason", out var fr)
            ? fr.GetString()
            : null;

        var content = candidate
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrEmpty(content))
        {
            if (finishReason == "MAX_TOKENS")
            {
                _logger.LogWarning("Gemini returned empty content (MAX_TOKENS)");
            }
            return null;
        }

        if (finishReason == "MAX_TOKENS")
        {
            _logger.LogWarning("Gemini response may be truncated (MAX_TOKENS), attempting parse anyway");
        }

        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```", StringComparison.Ordinal);

            if (end > start)
            {
                content = content[start..end].Trim();
            }
        }

        try
        {
            return JsonSerializer.Deserialize<MatchScoreDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize LLM response: {Content}", content);
            return null;
        }
    }
    private static string StripHtml(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return string.Empty;
    text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
    text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    return text;
}
}
