using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;

namespace CarRental.Application.Modules.Pricing;

/// <summary>Cấu hình phí cố định (section "Pricing" trong appsettings). Không có API bản đồ nên phí giao nhận là phí cố định mỗi chiều.</summary>
public class PricingOptions
{
    public decimal DriverFeePerDay { get; set; } = 500_000m;
    public decimal DriverFeePerHour { get; set; } = 80_000m;
    public decimal DeliveryFee { get; set; } = 150_000m;
    public decimal OneWayFee { get; set; } = 200_000m;
}

/// <summary>Lựa chọn của khách khi đặt xe (mã giảm giá, tài xế, giao nhận tận nơi, chi nhánh).</summary>
public record BookingOptions(
    string? CouponCode = null,
    bool WithDriver = false,
    string? DeliveryAddress = null,
    string? ReturnAddress = null,
    int? PickupBranchId = null,
    int? ReturnBranchId = null);

/// <summary>CustomerId null khi chỉ báo giá (không kiểm giới hạn mỗi khách).</summary>
public record ChargeContext(int? CustomerId, Car Car, DateTime Start, DateTime End, RentalMode Mode, decimal RentalAmount, BookingOptions Options);

public record ChargeLine(ChargeKind Kind, string Name, decimal Amount, int? CouponId = null, int? SurchargeId = null);

public record ChargeLineDto(string Kind, string Name, decimal Amount);

public record CouponDto(
    int Id, string Code, CouponType Type, decimal Value, decimal? MaxDiscount, decimal? MinOrderAmount,
    DateTime ValidFrom, DateTime ValidTo, int? UsageLimit, int UsedCount, int PerCustomerLimit, bool IsActive);

public record SaveCouponRequest(
    [Required, StringLength(40, MinimumLength = 3)] string Code,
    CouponType Type,
    [Range(0.01, 1_000_000_000)] decimal Value,
    [Range(0, 1_000_000_000)] decimal? MaxDiscount,
    [Range(0, 1_000_000_000)] decimal? MinOrderAmount,
    DateTime ValidFrom,
    DateTime ValidTo,
    [Range(1, 1_000_000)] int? UsageLimit,
    [Range(1, 1000)] int PerCustomerLimit = 1,
    bool IsActive = true);

/// <summary>Kết quả kiểm tra mã cho khách (chưa gắn đơn): không lộ UsedCount.</summary>
public record CouponCheckDto(string Code, CouponType Type, decimal Value, decimal? MaxDiscount, decimal? MinOrderAmount, DateTime ValidTo);

public record SurchargeDto(int Id, string Name, SurchargeType Type, decimal Amount, bool AutoApply, bool IsActive);

public record SaveSurchargeRequest(
    [Required, StringLength(100)] string Name,
    SurchargeType Type,
    [Range(0, 1_000_000_000)] decimal Amount,
    bool AutoApply,
    bool IsActive = true);

public record BranchDto(int Id, string Name, string Address, string? Phone, bool IsActive);

public record SaveBranchRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(300)] string Address,
    [StringLength(20)] string? Phone,
    bool IsActive = true);
