namespace CarRental.Infrastructure.Ai;

public sealed class AiSettings
{
    public const string SectionName = "AiService";
    public string BaseUrl { get; set; } = "http://localhost:5002";
    public int TimeoutSeconds { get; set; } = 2;
    public int RetryCount { get; set; } = 2;
}
