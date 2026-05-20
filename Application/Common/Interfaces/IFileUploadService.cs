using Microsoft.AspNetCore.Http;

namespace Application.Common.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string folder = "uploads");
    void DeleteFile(string fileUrl);
}
