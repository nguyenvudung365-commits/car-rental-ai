using CarRental.Application.Common;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Customers;

public interface ICustomerAdminService
{
    Task<(List<CustomerListItemDto> Items, int TotalCount)> ListAsync(CustomerListFilterRequest filter);
    Task<CustomerDetailDto> GetAsync(int id);
}

public class CustomerAdminService : ICustomerAdminService
{
    private readonly ICustomerAdminRepository _admin;
    private readonly ICustomerRepository _customers;
    private readonly IDocumentRepository _documents;
    private readonly ILoyaltyService _loyalty;
    private readonly ICustomerScreening _screening;
    private readonly ICurrentUser _currentUser;

    public CustomerAdminService(ICustomerAdminRepository admin, ICustomerRepository customers, IDocumentRepository documents,
        ILoyaltyService loyalty, ICustomerScreening screening, ICurrentUser currentUser)
    {
        _admin = admin;
        _customers = customers;
        _documents = documents;
        _loyalty = loyalty;
        _screening = screening;
        _currentUser = currentUser;
    }

    public async Task<(List<CustomerListItemDto> Items, int TotalCount)> ListAsync(CustomerListFilterRequest filter)
    {
        var (rows, total) = await _admin.SearchAsync(
            filter with { Search = filter.Search?.Trim() }, Math.Max(filter.Page, 1), Math.Clamp(filter.PageSize, 1, 100));
        var items = rows.Select(r => new CustomerListItemDto(
            r.Customer.Id, r.Customer.FullName, r.Customer.Email, r.Customer.PhoneNumber,
            LoyaltyPolicy.TierFor(r.Customer.LifetimePoints), r.Customer.LoyaltyPoints, r.DocumentsStatus,
            r.BlacklistLevel, r.Customer.IsLocked, r.BookingCount, r.TotalSpent, r.Customer.CreatedAt)).ToList();
        return (items, total);
    }

    /// <summary>Staff chỉ thấy 4 số cuối của số CCCD/GPLX.</summary>
    private static string MaskNumber(string number)
        => number.Length <= 4 ? number : new string('*', number.Length - 4) + number[^4..];

    public async Task<CustomerDetailDto> GetAsync(int id)
    {
        var c = await _customers.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy khách hàng.");
        // Trang này chỉ dành cho khách; tài khoản Staff/Admin không lộ qua đây.
        if (c.Role != UserRole.Customer) throw new NotFoundException("Không tìm thấy khách hàng.");
        var docs = await _documents.GetByCustomerAsync(id);
        var (bookingCount, totalSpent) = await _admin.GetStatsAsync(id);
        var loyalty = await _loyalty.GetAsync(id);
        var isAdmin = _currentUser.IsAdmin;

        return new CustomerDetailDto(
            c.Id, c.FullName, c.Email, c.PhoneNumber,
            isAdmin ? c.IdentityNumber : null, isAdmin ? c.Address : null, c.AvatarKey,
            c.IsLocked, isAdmin ? c.LockReason : null, c.LockedAt, c.CreatedAt,
            bookingCount, totalSpent, DocumentService.Overall(docs),
            await _screening.ScreenAsync(id),
            isAdmin ? loyalty : loyalty with { History = [] },
            docs.Select(d => DocumentService.ToDto(d) with { Number = isAdmin ? d.Number : MaskNumber(d.Number) }).ToList(),
            await _admin.GetRecentBookingsAsync(id, 10));
    }
}
