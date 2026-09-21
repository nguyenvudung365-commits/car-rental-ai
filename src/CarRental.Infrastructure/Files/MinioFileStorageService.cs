using CarRental.Application.Common;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CarRental.Infrastructure.Files;

public sealed class MinioFileStorageService : IFileStorageService
{
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

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey)
            || objectKey.Contains("..", StringComparison.Ordinal)
            || objectKey.Any(char.IsControl)
            || Uri.TryCreate(objectKey, UriKind.Absolute, out _)
            || !objectKey.StartsWith("cars/", StringComparison.Ordinal))
            throw new ArgumentException("Invalid storage object key.", nameof(objectKey));

        return _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_settings.Bucket)
            .WithObject(objectKey), cancellationToken);
    }
}
