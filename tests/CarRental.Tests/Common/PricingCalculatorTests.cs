using CarRental.Application.Common;

namespace CarRental.Tests.Common;

public class PricingCalculatorTests
{
    [Fact]
    public void Override_wins_over_predicted_and_base()
    {
        var result = PricingCalculator.ResolveFinalPricePerDay(600_000m, 700_000m, 500_000m);
        Assert.Equal(600_000m, result);
    }

    [Fact]
    public void Predicted_used_when_override_is_null()
    {
        var result = PricingCalculator.ResolveFinalPricePerDay(null, 700_000m, 500_000m);
        Assert.Equal(700_000m, result);
    }

    [Fact]
    public void Base_used_when_override_and_predicted_are_null()
    {
        var result = PricingCalculator.ResolveFinalPricePerDay(null, null, 500_000m);
        Assert.Equal(500_000m, result);
    }

    [Fact]
    public void Clamps_below_min_to_400000()
    {
        var result = PricingCalculator.ResolveFinalPricePerDay(300_000m, 700_000m, 500_000m);
        Assert.Equal(400_000m, result);
    }

    [Fact]
    public void Clamps_above_max_to_1500000()
    {
        var result = PricingCalculator.ResolveFinalPricePerDay(2_000_000m, 700_000m, 500_000m);
        Assert.Equal(1_500_000m, result);
    }
}
