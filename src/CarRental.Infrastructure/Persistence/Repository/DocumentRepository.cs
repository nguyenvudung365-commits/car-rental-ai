using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;
    public DocumentRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<CustomerDocument> Active => _context.Set<CustomerDocument>().Where(d => !d.IsDeleted);

    public Task<List<CustomerDocument>> GetByCustomerAsync(int customerId)
        => Active.AsNoTracking().Where(d => d.CustomerId == customerId).OrderBy(d => d.Type).ToListAsync();

    public Task<CustomerDocument?> GetActiveAsync(int customerId, DocumentType type)
        => Active.FirstOrDefaultAsync(d => d.CustomerId == customerId && d.Type == type);

    public Task<CustomerDocument?> GetByIdAsync(int id)
        => Active.Include(d => d.Customer).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<(List<CustomerDocument> Items, int TotalCount)> GetQueueAsync(DocumentStatus? status, int page, int pageSize)
    {
        var query = Active.Include(d => d.Customer).AsNoTracking();
        if (status.HasValue) query = query.Where(d => d.Status == status);

        var total = await query.CountAsync();
        // Hàng đợi duyệt: đơn nộp cũ nhất lên trước; các trạng thái khác: mới nhất lên trước.
        var ordered = status == DocumentStatus.ChoDuyet
            ? query.OrderBy(d => d.CreatedAt).ThenBy(d => d.Id)
            : query.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id);
        return (await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(), total);
    }

    public async Task AddAsync(CustomerDocument document) => await _context.Set<CustomerDocument>().AddAsync(document);
}
