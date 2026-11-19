namespace CarRental.Application.Modules.Customers;

public interface ILoyaltyService
{
    Task<LoyaltyDto> GetAsync(int customerId);
    /// <summary>Cộng điểm cho đơn đã trả xe. Idempotent: đơn đã được cộng thì bỏ qua.</summary>
    Task AwardForBookingAsync(int customerId, int bookingId, decimal orderValue);
    Task<LoyaltyDto> AdjustAsync(int customerId, AdjustLoyaltyRequest request);
}
