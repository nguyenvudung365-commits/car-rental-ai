using CarRental.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CarRental.Infrastructure.Files;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _webRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var uploadDir = Path.Combine(_webRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(uploadDir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{fileName}";
    }

    public void Delete(string relativePath)
    {
        var fullPath = Path.Combine(_webRootPath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}