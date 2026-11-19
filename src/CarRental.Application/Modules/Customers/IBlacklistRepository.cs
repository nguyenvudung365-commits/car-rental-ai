using CarRental.Domain.Modules.Customers;

namespace CarRental.Application.Modules.Customers;

public interface IBlacklistRepository
{
    Task<BlacklistEntry?> GetByIdAsync(int id);
    Task<(List<BlacklistEntry> Items, int TotalCount)> GetFilteredAsync(bool activeOnly, int page, int pageSize);
    /// <summary>Các bản ghi đang hiệu lực khớp theo CustomerId, CCCD hoặc SĐT.</summary>
    Task<List<BlacklistEntry>> FindActiveMatchesAsync(int customerId, string? identityNumber, string? phoneNumber);
    Task AddAsync(BlacklistEntry entry);
}
