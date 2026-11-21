using CarRental.Application.Common;
using CarRental.Domain.Modules.Customers;

namespace CarRental.Application.Modules.Customers;

public class BlacklistService : IBlacklistService, ICustomerScreening
{
    private readonly IBlacklistRepository _blacklist;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public BlacklistService(IBlacklistRepository blacklist, ICustomerRepository customers, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _blacklist = blacklist;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<(List<BlacklistEntryDto> Items, int TotalCount)> ListAsync(BlacklistFilterRequest filter)
    {
        var (items, total) = await _blacklist.GetFilteredAsync(
            filter.ActiveOnly ?? true, Math.Max(filter.Page, 1), Math.Clamp(filter.PageSize, 1, 100));
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<BlacklistEntryDto> CreateAsync(CreateBlacklistRequest request)
    {
        var identity = IdentityNormalizer.Cccd(request.IdentityNumber);
        var phone = IdentityNormalizer.Phone(request.PhoneNumber);
        if (request.CustomerId is null && identity is null && phone is null)
            throw new BadRequestException("BLACKLIST_KEY_REQUIRED", "Cần ít nhất một thông tin nhận diện: khách hàng, CCCD hoặc số điện thoại.");
        if (!Enum.IsDefined(request.Level))
            throw new BadRequestException("INVALID_LEVEL", "Mức chặn không hợp lệ.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("REASON_REQUIRED", "Cần nhập lý do.");

        string? customerName = null;
        if (request.CustomerId is { } customerId)
        {
            var customer = await _customers.GetByIdAsync(customerId)
                ?? throw new NotFoundException("Không tìm thấy khách hàng.");
            customerName = customer.FullName;
            // Chặn theo tài khoản thì kèm luôn CCCD/SĐT để khách tạo tài khoản mới vẫn bị bắt.
            identity ??= IdentityNormalizer.Cccd(customer.IdentityNumber);
            phone ??= IdentityNormalizer.Phone(customer.PhoneNumber);
        }

        var entry = new BlacklistEntry
        {
            CustomerId = request.CustomerId,
            IdentityNumber = identity,
            PhoneNumber = phone,
            Level = request.Level,
            Reason = request.Reason.Trim(),
            CreatedById = _currentUser.UserId ?? 0
        };
        await _blacklist.AddAsync(entry);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(entry) with { CustomerName = customerName };
    }

    public async Task RemoveAsync(int id, RemoveBlacklistRequest request)
    {
        var entry = await _blacklist.GetByIdAsync(id)
            ?? throw new NotFoundException("Không tìm thấy bản ghi chặn.");
        if (entry.RemovedAt is not null)
            throw new ConflictException("ALREADY_REMOVED", "Bản ghi đã được gỡ trước đó.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("REASON_REQUIRED", "Cần nhập lý do gỡ chặn.");

        entry.RemovedAt = DateTime.UtcNow;
        entry.RemovedById = _currentUser.UserId;
        entry.RemoveReason = request.Reason.Trim();
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<ScreeningResult> ScreenAsync(int customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId);
        if (customer is null) return new ScreeningResult(null, null);

        var matches = await _blacklist.FindActiveMatchesAsync(
            customerId, IdentityNormalizer.Cccd(customer.IdentityNumber), IdentityNormalizer.Phone(customer.PhoneNumber));
        var worst = matches.OrderByDescending(m => m.Level).ThenByDescending(m => m.CreatedAt).FirstOrDefault();
        return worst is null ? new ScreeningResult(null, null) : new ScreeningResult(worst.Level, worst.Reason);
    }

    private static BlacklistEntryDto ToDto(BlacklistEntry e) => new(
        e.Id, e.CustomerId, e.Customer?.FullName, e.IdentityNumber, e.PhoneNumber, e.Level, e.Reason,
        e.CreatedById, e.CreatedAt, e.RemovedAt, e.RemovedById, e.RemoveReason);
}
