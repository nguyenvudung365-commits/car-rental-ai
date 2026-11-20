using CarRental.Application.Modules.Audit;
using CarRental.Domain.Modules.Audit;
using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Audit;

public class AuditLogReader : IAuditLogReader
{
    private readonly ApplicationDbContext _context;
    public AuditLogReader(ApplicationDbContext context) => _context = context;

    public async Task<(List<AuditLogDto> Items, int TotalCount)> QueryAsync(AuditLogFilterRequest f)
    {
        var page = Math.Max(f.Page, 1);
        var pageSize = Math.Clamp(f.PageSize, 1, 100);

        var query = _context.Set<AuditLog>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.EntityType)) query = query.Where(a => a.EntityType == f.EntityType);
        if (!string.IsNullOrWhiteSpace(f.EntityId)) query = query.Where(a => a.EntityId == f.EntityId);
        if (f.ActorId.HasValue) query = query.Where(a => a.ActorId == f.ActorId);
        if (!string.IsNullOrWhiteSpace(f.Action)) query = query.Where(a => a.Action.StartsWith(f.Action));
        if (f.From.HasValue) query = query.Where(a => a.CreatedAt >= f.From);
        if (f.To.HasValue) query = query.Where(a => a.CreatedAt <= f.To);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.ActorId,
                _context.Set<Customer>().Where(c => c.Id == a.ActorId).Select(c => c.FullName).FirstOrDefault(),
                a.ActorRole, a.Action, a.EntityType, a.EntityId, a.Changes, a.IpAddress, a.CreatedAt))
            .ToListAsync();
        return (items, total);
    }
}
