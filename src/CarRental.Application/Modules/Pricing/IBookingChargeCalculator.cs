using CarRental.Domain.Modules.Pricing;

namespace CarRental.Application.Modules.Pricing;

/// <summary>Khung tính phí dùng chung: phụ phí tự động, tài xế, giao nhận, một chiều rồi mới áp mã giảm giá.</summary>
public interface IBookingChargeCalculator
{
    Task<List<ChargeLine>> CalculateAsync(ChargeContext ctx);

    /// <summary>
    /// Gọi trong transaction tạo đơn: trừ lượt mã (409 COUPON_EXHAUSTED/COUPON_ALREADY_USED), trả các BookingCharge để gắn vào đơn.
    /// Lượt mã được trừ bằng UPDATE có điều kiện nên hai request đồng thời xếp hàng theo khóa dòng mã.
    /// </summary>
    Task<List<BookingCharge>> RedeemAsync(int customerId, List<ChargeLine> lines);

    Task<(int? Pickup, int? Return)> ResolveBranchesAsync(int? carBranchId, BookingOptions options);

    /// <summary>Trả lại lượt mã khi đơn bị hủy.</summary>
    Task ReleaseAsync(IEnumerable<BookingCharge> charges);
}

/// <summary>Mặc định khi BookingService được tạo không kèm khung tính phí (test cũ): không có dòng phí nào.</summary>
public sealed class NoCharges : IBookingChargeCalculator
{
    public static readonly NoCharges Instance = new();
    public Task<List<ChargeLine>> CalculateAsync(ChargeContext ctx) => Task.FromResult(new List<ChargeLine>());
    public Task<List<BookingCharge>> RedeemAsync(int customerId, List<ChargeLine> lines) => Task.FromResult(new List<BookingCharge>());
    public Task ReleaseAsync(IEnumerable<BookingCharge> charges) => Task.CompletedTask;
    public Task<(int? Pickup, int? Return)> ResolveBranchesAsync(int? carBranchId, BookingOptions o) => Task.FromResult((carBranchId, carBranchId));
}
