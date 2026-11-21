using CarRental.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.Exceptions;
using Minio.DataModel.Args;

namespace CarRental.Infrastructure.Files;

public sealed class MinioFileStorageService : IFileStorageService
{
    // Prefix cho phép xóa/đọc: cars/ và avatars/ công khai, private/ chỉ truy cập qua API có kiểm quyền.
    private static readonly string[] AllowedPrefixes = ["cars/", "avatars/", "private/"];

    private readonly MinioSettings _settings;
    private readonly IMinioClient _client;

    public MinioFileStorageService(IOptions<MinioSettings> options)
    {
        _settings = options.Value;
        var client = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey);

        if (_settings.UseSsl)
            client = client.WithSSL();

        _client = client.Build();
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        string folder,
        CancellationToken cancellationToken = default)
    {
        await WebpValidator.ValidateAsync(content, fileName, contentType, length, cancellationToken);

        var normalizedFolder = folder.Trim('/').Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalizedFolder)
            || normalizedFolder.Contains("..", StringComparison.Ordinal)
            || normalizedFolder.Any(char.IsControl))
            throw new ArgumentException("Invalid storage folder.", nameof(folder));

        var objectKey = $"{normalizedFolder}/{Guid.NewGuid():N}.webp";
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_settings.Bucket)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType("image/webp"), cancellationToken);

        return objectKey;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        await using var stream = file.OpenReadStream();
        return await SaveAsync(stream, file.FileName, file.ContentType, file.Length, folder);
    }

    public void Delete(string relativePath)
    {
        DeleteAsync(relativePath).GetAwaiter().GetResult();
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey)
            || objectKey.Contains("..", StringComparison.Ordinal)
            || objectKey.Any(char.IsControl)
            || Uri.TryCreate(objectKey, UriKind.Absolute, out _)
            || !AllowedPrefixes.Any(p => objectKey.StartsWith(p, StringComparison.Ordinal)))
            throw new ArgumentException("Invalid storage object key.", nameof(objectKey));

        return _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_settings.Bucket)
            .WithObject(objectKey), cancellationToken);
    }

    /// <summary>Đọc object vào bộ nhớ (ảnh WebP ≤ 5 MB) để API stream lại sau khi kiểm quyền.</summary>
    public async Task<(Stream Content, string ContentType)> OpenReadAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || key.Contains("..", StringComparison.Ordinal)
            || key.Any(char.IsControl)
            || !AllowedPrefixes.Any(p => key.StartsWith(p, StringComparison.Ordinal)))
            throw new ArgumentException("Invalid storage object key.", nameof(key));

        var buffer = new MemoryStream();
        try
        {
            await _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(key)
                .WithCallbackStream(s => s.CopyTo(buffer)));
        }
        catch (ObjectNotFoundException)
        {
            throw new NotFoundException("Không tìm thấy tệp.");
        }
        buffer.Position = 0;
        return (buffer, "image/webp");
    }
}
