namespace JobPortal.API.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public long MaxImageBytes { get; set; } = 2 * 1024 * 1024;
    public string[] AllowedImageExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
    public long MaxResumeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedResumeExtensions { get; set; } = [".pdf"];
    public string PublicBaseUrl { get; set; } = string.Empty;
}
