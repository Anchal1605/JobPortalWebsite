using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace JobPortal.API.Services;

public class ResumeTextExtractor
{
    private const int MaxResumeChars = 6000;

    private readonly FileStorageService _fileStorage;
    private readonly ILogger<ResumeTextExtractor> _logger;

    public ResumeTextExtractor(
        FileStorageService fileStorage,
        ILogger<ResumeTextExtractor> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public string? ExtractFromPublicUrl(string? resumeUrl)
    {
        var physicalPath = _fileStorage.TryGetLocalPhysicalPath(resumeUrl);
        if (physicalPath == null)
        {
            return null;
        }

        if (!physicalPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return ExtractAndRedact(() =>
            {
                var builder = new StringBuilder();
                using var document = PdfDocument.Open(physicalPath);
                foreach (var page in document.GetPages())
                {
                    builder.AppendLine(page.Text);
                }

                return builder.ToString();
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract resume text from URL");
            return null;
        }
    }

    public string? ExtractFromFormFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = file.OpenReadStream();
            return ExtractAndRedact(() =>
            {
                var builder = new StringBuilder();
                using var document = PdfDocument.Open(stream);
                foreach (var page in document.GetPages())
                {
                    builder.AppendLine(page.Text);
                }

                return builder.ToString();
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract resume text from upload");
            return null;
        }
    }

    private static string? ExtractAndRedact(Func<string> readText)
    {
        var text = readText().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = RedactPii(text);

        if (text.Length > MaxResumeChars)
        {
            text = text[..MaxResumeChars] + "...";
        }

        return text;
    }

    private static string RedactPii(string text)
    {
        // Email
        text = Regex.Replace(
            text,
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
            "[email redacted]");

        // Phone (common formats)
        text = Regex.Replace(
            text,
            @"(\+?\d{1,3}[\s.-]?)?(\(?\d{3}\)?[\s.-]?)?\d{3}[\s.-]?\d{4}",
            "[phone redacted]");

        // Links
        text = Regex.Replace(
            text,
            @"https?://\S+|www\.\S+",
            "[link redacted]",
            RegexOptions.IgnoreCase);

        return text;
    }
}
