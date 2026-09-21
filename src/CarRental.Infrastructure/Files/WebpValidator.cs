namespace CarRental.Infrastructure.Files;

public static class WebpValidator
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    public static async Task ValidateAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > MaxFileSize)
            throw new WebpValidationException("Ảnh phải có dung lượng từ 1 byte đến 5 MB.");

        if (!string.Equals(Path.GetExtension(fileName), ".webp", StringComparison.OrdinalIgnoreCase))
            throw new WebpValidationException("Chỉ chấp nhận ảnh WebP.");

        if (!string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase))
            throw new WebpValidationException("Content-Type phải là image/webp.");

        if (!content.CanSeek)
            throw new WebpValidationException("Không thể kiểm tra định dạng ảnh.");

        var header = new byte[12];
        content.Position = 0;
        var read = await content.ReadAsync(header.AsMemory(), cancellationToken);
        content.Position = 0;

        var isWebp = read == header.Length
            && header[0] == (byte)'R'
            && header[1] == (byte)'I'
            && header[2] == (byte)'F'
            && header[3] == (byte)'F'
            && header[8] == (byte)'W'
            && header[9] == (byte)'E'
            && header[10] == (byte)'B'
            && header[11] == (byte)'P';

        if (!isWebp)
            throw new WebpValidationException("Nội dung file không phải WebP hợp lệ.");
    }
}
