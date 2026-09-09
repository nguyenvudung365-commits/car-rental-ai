namespace CarRental.Application.Common;

/// <summary>
/// Công thức giá đã chốt (docs/architecture.md mục 5).
/// FinalPricePerDay = COALESCE(OverridePrice, PredictedPrice, BasePricePerDay)
/// rồi chặn [400.000đ, 1.500.000đ] / ngày.
/// </summary>
public static class PricingCalculator
{
    public const decimal MinPricePerDay = 400_000m;
    public const decimal MaxPricePerDay = 1_500_000m;

    public static decimal ResolveFinalPricePerDay(
        decimal? overridePrice,
        decimal? predictedPrice,
        decimal basePricePerDay)
    {
        var raw = overridePrice ?? predictedPrice ?? basePricePerDay;
        if (raw < MinPricePerDay) return MinPricePerDay;
        if (raw > MaxPricePerDay) return MaxPricePerDay;
        return raw;
    }
}
