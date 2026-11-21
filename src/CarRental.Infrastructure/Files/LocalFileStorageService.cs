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
        // Local lưu dưới wwwroot (UseStaticFiles công khai) nên không được chứa file riêng tư; prefix private/ chỉ dùng với MinIO.
        if (folder.TrimStart('/').StartsWith("private/", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("LocalFileStorageService không hỗ trợ prefix private/ (sẽ bị công khai qua /uploads).");
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

    public Task<(Stream Content, string ContentType)> OpenReadAsync(string key)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_webRootPath, key.TrimStart('/')));
        if (key.Contains("..", StringComparison.Ordinal) || !fullPath.StartsWith(_webRootPath, StringComparison.Ordinal))
            throw new ArgumentException("Invalid storage object key.", nameof(key));
        if (!File.Exists(fullPath)) throw new NotFoundException("Không tìm thấy tệp.");
        return Task.FromResult<(Stream, string)>((new FileStream(fullPath, FileMode.Open, FileAccess.Read), "image/webp"));
    }
}
