using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class DepositRepository : IDepositRepository
{
    private readonly ApplicationDbContext _context;
    public DepositRepository(ApplicationDbContext context) => _context = context;

    public async Task AddAsync(DepositTransaction transaction) => await _context.Set<DepositTransaction>().AddAsync(transaction);

    public Task<List<DepositTransaction>> ListByBookingAsync(int bookingId)
        => _context.Set<DepositTransaction>().AsNoTracking().Where(t => t.BookingId == bookingId && !t.IsDeleted)
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).ToListAsync();

    public Task LockBookingAsync(int bookingId) =>
        _context.Bookings
            .FromSqlInterpolated($"SELECT * FROM Bookings WITH (UPDLOCK, ROWLOCK) WHERE Id = {bookingId}")
            .AsNoTracking()
            .Select(b => b.Id)
            .FirstOrDefaultAsync();
}
