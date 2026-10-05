namespace CarRental.Application.Modules.Bookings;

public interface IBookingService
{
    Task<BookingDto> CreateAsync(int customerId, CreateBookingRequest request);
    Task<(List<BookingDto> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null);
    Task<BookingDto> GetByIdAsync(int id, int? customerId = null);
    Task<BookingDto> ConfirmAsync(int id);
    Task<BookingDto> CancelAsync(int id, string? reason);
}
