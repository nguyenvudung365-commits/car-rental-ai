using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Common;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folder);
    void Delete(string relativePath);
}
