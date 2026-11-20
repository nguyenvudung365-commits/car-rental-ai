using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Common;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folder);
    void Delete(string relativePath);
    /// <summary>Đọc object theo key (dùng cho file riêng tư stream qua API có kiểm quyền).</summary>
    Task<(Stream Content, string ContentType)> OpenReadAsync(string key);
}
