using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;

namespace CarRental.Application.Modules.Customers;

public interface IDocumentRepository
{
    /// <summary>Giấy tờ hiệu lực (chưa soft-delete) của khách, không theo dõi thay đổi.</summary>
    Task<List<CustomerDocument>> GetByCustomerAsync(int customerId);
    Task<CustomerDocument?> GetActiveAsync(int customerId, DocumentType type);
    Task<CustomerDocument?> GetByIdAsync(int id);
    Task<(List<CustomerDocument> Items, int TotalCount)> GetQueueAsync(DocumentStatus? status, int page, int pageSize);
    Task AddAsync(CustomerDocument document);
}
