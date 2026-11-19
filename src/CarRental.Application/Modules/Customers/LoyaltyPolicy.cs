using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Customers;

/// <summary>
/// Luật điểm thưởng: điểm = floor(1% giá trị đơn), 1 điểm = 1đ. Hạng theo tổng điểm tích lũy trọn đời
/// (tương ứng chi tiêu 10/30/80 triệu).
/// </summary>
public static class LoyaltyPolicy
{
    public const decimal EarnRate = 0.01m;
    public const int BacThreshold = 100_000;
    public const int VangThreshold = 300_000;
    public const int KimCuongThreshold = 800_000;

    public static int PointsFor(decimal orderValue)
        => orderValue <= 0 ? 0 : (int)Math.Floor(orderValue * EarnRate);

    public static MemberTier TierFor(int lifetimePoints) => lifetimePoints switch
    {
        >= KimCuongThreshold => MemberTier.KimCuong,
        >= VangThreshold => MemberTier.Vang,
        >= BacThreshold => MemberTier.Bac,
        _ => MemberTier.ThanhVien
    };

    /// <summary>Khoảng điểm tích lũy [Min, MaxExclusive) của một hạng (MaxExclusive null = không giới hạn).</summary>
    public static (int Min, int? MaxExclusive) RangeOf(MemberTier tier) => tier switch
    {
        MemberTier.ThanhVien => (0, BacThreshold),
        MemberTier.Bac => (BacThreshold, VangThreshold),
        MemberTier.Vang => (VangThreshold, KimCuongThreshold),
        _ => (KimCuongThreshold, null)
    };

    /// <summary>Hạng kế tiếp và số điểm còn thiếu; null khi đã ở hạng cao nhất.</summary>
    public static (MemberTier Next, int PointsToNext)? NextTier(int lifetimePoints)
    {
        var next = TierFor(lifetimePoints) + 1;
        return next > MemberTier.KimCuong ? null : (next, RangeOf(next).Min - lifetimePoints);
    }
}
