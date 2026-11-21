using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class HandoverRepository : IHandoverRepository
{
    private readonly ApplicationDbContext _context;
    public HandoverRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<HandoverRecord> Records => _context.Set<HandoverRecord>().Include(h => h.Photos).Where(h => !h.IsDeleted);

    public async Task AddAsync(HandoverRecord record) => await _context.Set<HandoverRecord>().AddAsync(record);

    public async Task AddPhotoAsync(HandoverPhoto photo) => await _context.Set<HandoverPhoto>().AddAsync(photo);

    public Task<HandoverRecord?> GetByIdAsync(int id) => Records.FirstOrDefaultAsync(h => h.Id == id);

    public Task<HandoverRecord?> GetByBookingAsync(int bookingId, HandoverType type)
        => Records.AsNoTracking().FirstOrDefaultAsync(h => h.BookingId == bookingId && h.Type == type);

    public Task<List<HandoverRecord>> ListByBookingAsync(int bookingId)
        => Records.AsNoTracking().Where(h => h.BookingId == bookingId).OrderBy(h => h.Type).ToListAsync();

    public Task<HandoverPhoto?> GetPhotoAsync(int photoId)
        => _context.Set<HandoverPhoto>().Include(p => p.HandoverRecord).FirstOrDefaultAsync(p => p.Id == photoId && !p.IsDeleted);

    public Task<bool> ExistsAsync(int bookingId, HandoverType type)
        => _context.Set<HandoverRecord>().AnyAsync(h => h.BookingId == bookingId && h.Type == type && !h.IsDeleted);
}
