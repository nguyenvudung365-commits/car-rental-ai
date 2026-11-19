using CarRental.Domain.Modules.Bookings;

namespace CarRental.Application.Modules.Bookings;

public interface IDepositRepository
{
    Task AddAsync(DepositTransaction transaction);
    Task<List<DepositTransaction>> ListByBookingAsync(int bookingId);
    /// <summary>Khóa dòng booking (UPDLOCK) trong transaction hiện tại để các thao tác cọc cùng đơn xếp hàng.</summary>
    Task LockBookingAsync(int bookingId);
}
