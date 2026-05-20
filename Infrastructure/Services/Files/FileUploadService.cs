using Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Files;

public class FileUploadService(IWebHostEnvironment webHostEnvironment) : IFileUploadService
{
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx", ".jpg", ".png", ".jpeg" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public async Task<string> UploadFileAsync(IFormFile file, string folder = "uploads")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty.");

        if (file.Length > MaxFileSize)
            throw new ArgumentException("File size exceeds 10 MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
            throw new ArgumentException($"File extension {extension} is not allowed.");

        // Additional magic bytes validation can be added here
        
        var uploadPath = Path.Combine(webHostEnvironment.WebRootPath, folder);
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative URL assuming standard setup (e.g. /uploads/filename.pdf)
        return $"/{folder}/{fileName}";
    }

    public void DeleteFile(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;
        
        var relativePath = fileUrl.TrimStart('/');
        var fullPath = Path.Combine(webHostEnvironment.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
