using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class BlacklistRepository : IBlacklistRepository
{
    private readonly ApplicationDbContext _context;
    public BlacklistRepository(ApplicationDbContext context) => _context = context;

    public Task<BlacklistEntry?> GetByIdAsync(int id)
        => _context.Set<BlacklistEntry>().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<(List<BlacklistEntry> Items, int TotalCount)> GetFilteredAsync(bool activeOnly, int page, int pageSize)
    {
        var query = _context.Set<BlacklistEntry>().Include(e => e.Customer).AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(e => e.RemovedAt == null);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<List<BlacklistEntry>> FindActiveMatchesAsync(int customerId, string? identityNumber, string? phoneNumber)
    {
        identityNumber = IdentityNormalizer.Cccd(identityNumber);
        phoneNumber = IdentityNormalizer.Phone(phoneNumber);
        return _context.Set<BlacklistEntry>().AsNoTracking()
            .Where(e => e.RemovedAt == null
                && (e.CustomerId == customerId
                    || (identityNumber != null && e.IdentityNumber == identityNumber)
                    || (phoneNumber != null && e.PhoneNumber == phoneNumber)))
            .ToListAsync();
    }

    public async Task AddAsync(BlacklistEntry entry) => await _context.Set<BlacklistEntry>().AddAsync(entry);
}
