using CarRental.Application.Common;

namespace CarRental.Tests.Common;

public class PricingCalculatorTests
{
    [Theory]
    [InlineData(600_000, 700_000, 500_000, 600_000)] // override wins
    [InlineData(null, 700_000, 500_000, 700_000)]    // predicted
    [InlineData(null, null, 500_000, 500_000)]       // fallback
    [InlineData(300_000, 700_000, 500_000, 400_000)] // clamp min
    [InlineData(2_000_000, 700_000, 500_000, 1_500_000)] // clamp max
    public void ResolveFinalPricePerDay_CoalesceAndClamp(decimal? ov, decimal? pred, decimal basePrice, decimal expected)
    {
        var result = PricingCalculator.ResolveFinalPricePerDay(ov, pred, basePrice);
        Assert.Equal(expected, result);
    }
}
