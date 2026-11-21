using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public interface IBookingExtensionRepository
{
    Task AddAsync(BookingExtension extension);
    /// <summary>Có kèm Booking (Customer, Car) để duyệt.</summary>
    Task<BookingExtension?> GetByIdAsync(int id);
    /// <summary>Chỉ đọc CarId (không tracking) để khóa xe trước khi nạp entity thật trong transaction.</summary>
    Task<int?> GetCarIdAsync(int id);
    Task<List<BookingExtension>> ListByBookingAsync(int bookingId);
    Task<List<BookingExtension>> ListByStatusAsync(ExtensionStatus status);
    Task<bool> HasPendingAsync(int bookingId);
}
