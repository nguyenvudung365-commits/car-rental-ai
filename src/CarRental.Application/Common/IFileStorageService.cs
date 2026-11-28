using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Common;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folder);
    void Delete(string relativePath);
    /// <summary>Đọc object theo key. Bên gọi chịu trách nhiệm kiểm tra quyền truy cập.</summary>
    Task<(Stream Content, string ContentType)> OpenReadAsync(string key);
}
