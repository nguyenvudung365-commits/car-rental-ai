namespace CarRental.Application.Common;

public sealed record PredictPriceRequest(
    string CarType,
    int CarAgeYears,
    int Seats,
    int RentalDays,
    decimal BasePricePerDay,
    DateTime StartDate);

public sealed record PredictPriceResult(decimal PredictedPrice, bool IsFallback);

/// <summary>
/// Cổng gọi FastAPI /predict-price.
/// Timeout 2s, retry 2 lần, lỗi → IsFallback = true + giá niêm yết.
/// Implement nằm ở Infrastructure (TV B gắn HttpClient).
/// </summary>
public interface IAiPricingClient
{
    Task<PredictPriceResult> PredictPriceAsync(
        PredictPriceRequest request,
        CancellationToken cancellationToken = default);
}
