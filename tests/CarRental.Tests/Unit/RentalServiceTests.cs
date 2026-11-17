using CarRental.Application.Modules.Bookings;
using Xunit;

namespace CarRental.Tests.Unit;

public sealed class RentalServiceTests
{
    private static readonly DateTime End = new(2030, 1, 10);

    [Fact]
    public void Return_OnTime_NoLateFee()
    {
        var (days, fee, final) = RentalService.CalculateReturn(End, End, 700_000, 2_100_000, 0);
        Assert.Equal(0, days);
        Assert.Equal(0m, fee);
        Assert.Equal(2_100_000m, final);
    }

    [Fact]
    public void Return_Early_NoLateFee()
    {
        var (days, fee, _) = RentalService.CalculateReturn(End, End.AddDays(-1), 700_000, 2_100_000, 0);
        Assert.Equal(0, days);
        Assert.Equal(0m, fee);
    }

    [Fact]
    public void Return_Late_ChargesFinalPricePerDayAndDamage()
    {
        var (days, fee, final) = RentalService.CalculateReturn(End, End.AddDays(2).AddHours(5), 700_000, 2_100_000, 300_000);
        Assert.Equal(2, days);
        Assert.Equal(1_400_000m, fee);
        Assert.Equal(3_800_000m, final);
    }
}
