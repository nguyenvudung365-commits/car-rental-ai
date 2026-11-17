using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id, bool includeDetails = false);
    Task<(List<Booking> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null);
    Task<bool> HasOverlapAsync(int carId, DateTime startDate, DateTime endDate, int? excludeBookingId = null);
    Task<List<(DateTime Start, DateTime End)>> GetActiveRangesAsync(int carId, DateTime from);
    Task AddAsync(Booking booking);
    void Update(Booking booking);
    Task<Payment?> GetPaymentByIdAsync(int id);
    Task AddPaymentAsync(Payment payment);
    Task AddReturnRecordAsync(ReturnRecord record);
}
