namespace CarRental.Application.Modules.Bookings;

/// <summary>Cọc bảo đảm: tách khỏi tiền thuê, không phải doanh thu.</summary>
public interface IDepositService
{
    Task<DepositSummaryDto> GetAsync(int bookingId, int? customerId = null);
    Task<DepositSummaryDto> CollectAsync(int bookingId, CollectDepositRequest request, int? staffId);
    Task<DepositSummaryDto> SettleAsync(int bookingId, SettleDepositRequest request, int? staffId);
    /// <summary>True khi xe không yêu cầu cọc hoặc đơn đã thu cọc (dùng cho chặn giao xe).</summary>
    Task<bool> IsSatisfiedForPickupAsync(int bookingId);
}
