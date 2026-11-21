using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;

namespace CarRental.Tests.Common;

public class LoyaltyPolicyTests
{
    [Theory]
    [InlineData(2_100_000, 21_000)]
    [InlineData(99_999, 999)]
    [InlineData(100, 1)]
    [InlineData(99, 0)]
    [InlineData(0, 0)]
    [InlineData(-500_000, 0)]
    public void PointsFor_IsFloorOfOnePercent(decimal value, int expected) => Assert.Equal(expected, LoyaltyPolicy.PointsFor(value));

    [Theory]
    [InlineData(0, MemberTier.ThanhVien)]
    [InlineData(99_999, MemberTier.ThanhVien)]
    [InlineData(100_000, MemberTier.Bac)]
    [InlineData(299_999, MemberTier.Bac)]
    [InlineData(300_000, MemberTier.Vang)]
    [InlineData(799_999, MemberTier.Vang)]
    [InlineData(800_000, MemberTier.KimCuong)]
    [InlineData(50_000_000, MemberTier.KimCuong)]
    public void TierFor_Boundaries(int lifetime, MemberTier expected) => Assert.Equal(expected, LoyaltyPolicy.TierFor(lifetime));

    [Fact]
    public void NextTier_ReportsMissingPoints_AndNullAtTop()
    {
        Assert.Equal((MemberTier.Bac, 1), LoyaltyPolicy.NextTier(99_999));
        Assert.Equal((MemberTier.Vang, 200_000), LoyaltyPolicy.NextTier(100_000));
        Assert.Equal((MemberTier.KimCuong, 1), LoyaltyPolicy.NextTier(799_999));
        Assert.Null(LoyaltyPolicy.NextTier(800_000));
    }
}
