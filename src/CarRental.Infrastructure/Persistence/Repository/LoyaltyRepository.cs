using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class LoyaltyRepository : ILoyaltyRepository
{
    private readonly ApplicationDbContext _context;
    public LoyaltyRepository(ApplicationDbContext context) => _context = context;

    public Task<bool> ExistsAsync(int bookingId, LoyaltyReason reason)
        => _context.Set<LoyaltyTransaction>().AnyAsync(t => t.BookingId == bookingId && t.Reason == reason);

    public Task ResetTrackingAsync()
    {
        _context.ChangeTracker.Clear();
        return Task.CompletedTask;
    }

    public async Task AddAsync(LoyaltyTransaction transaction) => await _context.Set<LoyaltyTransaction>().AddAsync(transaction);

    public Task<List<LoyaltyTransaction>> GetRecentAsync(int customerId, int take)
        => _context.Set<LoyaltyTransaction>().AsNoTracking().Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Take(take).ToListAsync();
}
