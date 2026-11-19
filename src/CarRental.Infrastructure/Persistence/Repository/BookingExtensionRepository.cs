using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class BookingExtensionRepository : IBookingExtensionRepository
{
    private readonly ApplicationDbContext _context;
    public BookingExtensionRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<BookingExtension> Active => _context.Set<BookingExtension>().Where(e => !e.IsDeleted);

    public async Task AddAsync(BookingExtension extension) => await _context.Set<BookingExtension>().AddAsync(extension);

    public Task<BookingExtension?> GetByIdAsync(int id)
        => Active.Include(e => e.Booking).ThenInclude(b => b!.Customer)
            .Include(e => e.Booking).ThenInclude(b => b!.Car)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<int?> GetCarIdAsync(int id)
        => await Active.AsNoTracking().Where(e => e.Id == id).Select(e => (int?)e.Booking!.CarId).FirstOrDefaultAsync();

    public Task<List<BookingExtension>> ListByBookingAsync(int bookingId)
        => Active.AsNoTracking().Where(e => e.BookingId == bookingId).OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).ToListAsync();

    public Task<List<BookingExtension>> ListByStatusAsync(ExtensionStatus status)
        => Active.AsNoTracking().Include(e => e.Booking).ThenInclude(b => b!.Customer)
            .Include(e => e.Booking).ThenInclude(b => b!.Car)
            .Where(e => e.Status == status).OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToListAsync();

    public Task<bool> HasPendingAsync(int bookingId)
        => Active.AnyAsync(e => e.BookingId == bookingId && e.Status == ExtensionStatus.ChoDuyet);
}
