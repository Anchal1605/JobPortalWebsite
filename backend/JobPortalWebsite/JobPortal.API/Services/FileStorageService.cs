using JobPortal.API.Options;
using Microsoft.Extensions.Options;

namespace JobPortal.API.Services
{
    public class FileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly FileStorageOptions _options;

        public FileStorageService(IWebHostEnvironment environment, IOptions<FileStorageOptions> options)
        {
            _environment = environment;
            _options = options.Value;
        }

        public void EnsureUploadDirectoriesExist()
        {
            EnsureDirectory("uploads/avatars");
            EnsureDirectory("uploads/logos");
            EnsureDirectory("uploads/resumes");
        }

        public string? ValidateImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "No file was uploaded";
            }

            if (file.Length > _options.MaxImageBytes)
            {
                var maxMb = _options.MaxImageBytes / (1024 * 1024);
                return $"Image must be {maxMb} MB or smaller";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return "Only JPG, PNG, and WEBP images are allowed";
            }

            if (!string.IsNullOrEmpty(file.ContentType) && !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "File must be an image";
            }

            return null;
        }

        public string? ValidateResume(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "No file was uploaded";
            }

            if (file.Length > _options.MaxResumeBytes)
            {
                var maxMb = _options.MaxResumeBytes / (1024 * 1024);
                return $"Résumé must be {maxMb} MB or smaller";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.AllowedResumeExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return "Only PDF résumés are allowed";
            }

            if (!string.IsNullOrEmpty(file.ContentType)
                && !file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return "File must be a PDF";
            }

            return null;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string subfolder, string fileNamePrefix)
        {
            return await SaveFileAsync(file, subfolder, fileNamePrefix);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subfolder, string fileNamePrefix)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeName = $"{fileNamePrefix}-{Guid.NewGuid():N}{extension}";
            var relativePath = Path.Combine("uploads", subfolder, safeName).Replace('\\', '/');
            var physicalDir = Path.Combine(_environment.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(physicalDir);

            var physicalPath = Path.Combine(physicalDir, safeName);
            await using var stream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
            return $"{baseUrl}/{relativePath}";
        }

        public void TryDeleteUploadedFile(string? publicUrl)
        {
            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                return;
            }

            var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
            if (!publicUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relative = publicUrl.Substring(baseUrl.Length).TrimStart('/');
            var physicalPath = Path.Combine(_environment.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        private void EnsureDirectory(string relativePath)
        {
            var path = Path.Combine(_environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(path);
        }
    }
}
