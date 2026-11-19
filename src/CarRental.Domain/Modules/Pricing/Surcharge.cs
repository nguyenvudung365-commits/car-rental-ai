using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Pricing;

/// <summary>Phụ phí cấu hình bởi Admin. AutoApply: tự cộng vào mọi đơn mới.</summary>
public class Surcharge : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public SurchargeType Type { get; set; }

    /// <summary>CoDinh: VNĐ; TheoNgay: VNĐ mỗi ngày (đơn theo giờ tính 1 ngày); PhanTram: % tiền thuê.</summary>
    public decimal Amount { get; set; }

    public bool AutoApply { get; set; }

    public bool IsActive { get; set; } = true;
}
