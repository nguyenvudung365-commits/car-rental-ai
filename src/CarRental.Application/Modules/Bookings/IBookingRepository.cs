using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id, bool includeDetails = false);
    Task<(List<Booking> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null);
    Task<bool> HasOverlapAsync(int carId, DateTime startDate, DateTime endDate, int? excludeBookingId = null);
    Task AddAsync(Booking booking);
    void Update(Booking booking);
}
