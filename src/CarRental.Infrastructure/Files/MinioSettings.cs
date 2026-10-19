namespace CarRental.Infrastructure.Files;

public class MinioSettings
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "car-rental";
    public bool UseSsl { get; set; }
    public string? PublicUrl { get; set; }
}
