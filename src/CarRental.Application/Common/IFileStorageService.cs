namespace CarRental.Application.Common;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, long length, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
