using CarRental.Application.Common;

namespace CarRental.Infrastructure.Ai;

/// <summary>
/// Stub T1-T2: luôn fallback giá niêm yết.
/// TV B thay bằng HttpClient (timeout 2s, retry 2, header X-Correlation-Id).
/// </summary>
public sealed class AiPricingClient : IAiPricingClient
{
    public Task<PredictPriceResult> PredictPriceAsync(
        PredictPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PredictPriceResult(request.BasePricePerDay, IsFallback: true));
    }
}
