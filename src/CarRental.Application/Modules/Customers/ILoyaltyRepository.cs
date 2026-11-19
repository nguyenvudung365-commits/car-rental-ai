using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;

namespace CarRental.Application.Modules.Customers;

public interface ILoyaltyRepository
{
    Task<bool> ExistsAsync(int bookingId, LoyaltyReason reason);
    Task AddAsync(LoyaltyTransaction transaction);
    /// <summary>Bỏ mọi thay đổi chưa lưu và nạp lại dữ liệu từ DB (dùng khi ghi điểm bị xung đột đồng thời).</summary>
    Task ResetTrackingAsync();
    Task<List<LoyaltyTransaction>> GetRecentAsync(int customerId, int take);
}
