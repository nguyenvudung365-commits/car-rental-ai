using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Pricing;

/// <summary>Mã giảm giá. Code viết hoa, duy nhất. UsedCount chỉ đổi bằng UPDATE có điều kiện (chặn vượt UsageLimit khi đồng thời).</summary>
public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public CouponType Type { get; set; }

    /// <summary>PhanTram: 1 đến 100. SoTien: số tiền VNĐ.</summary>
    public decimal Value { get; set; }

    public decimal? MaxDiscount { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public int PerCustomerLimit { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
