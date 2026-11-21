using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public interface IHandoverRepository
{
    Task AddAsync(HandoverRecord record);
    Task AddPhotoAsync(HandoverPhoto photo);
    Task<HandoverRecord?> GetByIdAsync(int id);
    Task<HandoverRecord?> GetByBookingAsync(int bookingId, HandoverType type);
    Task<List<HandoverRecord>> ListByBookingAsync(int bookingId);
    Task<HandoverPhoto?> GetPhotoAsync(int photoId);
    Task<bool> ExistsAsync(int bookingId, HandoverType type);
}
