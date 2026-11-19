using CarRental.Application.Common;
using CarRental.Domain.Common;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Application.Modules.Customers;

public class UserAdminService : IUserAdminService
{
    private readonly ICustomerRepository _customers;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UserAdminService(ICustomerRepository customers, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _customers = customers;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<(List<UserSummaryDto> Items, int TotalCount)> ListAsync(UserListFilterRequest filter)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var (items, total) = await _customers.SearchAsync(filter.Role, filter.Search?.Trim(), filter.IsLocked, page, pageSize);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<UserSummaryDto> CreateStaffAsync(CreateStaffRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _customers.GetByEmailAsync(email) is not null)
            throw new ConflictException("EMAIL_EXISTS", "Email đã được sử dụng.");
        if (await _customers.GetByPhoneNumberAsync(request.PhoneNumber) is not null)
            throw new ConflictException("PHONE_EXISTS", "Số điện thoại đã được sử dụng.");

        var staff = new CustomerEntity
        {
            FullName = request.FullName,
            Email = email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Staff
        };
        await _customers.AddAsync(staff);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(staff);
    }

    public async Task ChangeRoleAsync(int id, ChangeRoleRequest request)
    {
        if (!Enum.IsDefined(request.Role))
            throw new BadRequestException("INVALID_ROLE", "Vai trò không hợp lệ.");
        var user = await GetAsync(id);
        if (user.Id == _currentUser.UserId)
            throw new ConflictException("SELF_CHANGE", "Không thể tự đổi vai trò của chính mình.");
        if (user.Role == UserRole.Admin && request.Role != UserRole.Admin)
            await EnsureNotLastAdminAsync();

        user.Role = request.Role;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task LockAsync(int id, LockUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("REASON_REQUIRED", "Cần nhập lý do khóa tài khoản.");
        var user = await GetAsync(id);
        if (user.Id == _currentUser.UserId)
            throw new ConflictException("SELF_LOCK", "Không thể tự khóa tài khoản của chính mình.");
        if (user.Role == UserRole.Admin && !user.IsLocked)
            await EnsureNotLastAdminAsync();

        user.IsLocked = true;
        user.LockedAt = DateTime.UtcNow;
        user.LockReason = request.Reason.Trim();
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UnlockAsync(int id)
    {
        var user = await GetAsync(id);
        user.IsLocked = false;
        user.LockedAt = null;
        user.LockReason = null;
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<CustomerEntity> GetAsync(int id)
        => await _customers.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy tài khoản.");

    // Admin đang hoạt động còn lại phải >= 1 sau thao tác.
    private async Task EnsureNotLastAdminAsync()
    {
        if (await _customers.CountActiveAdminsAsync() <= 1)
            throw new ConflictException("LAST_ADMIN", "Không thể thao tác với quản trị viên cuối cùng.");
    }

    private static UserSummaryDto ToDto(CustomerEntity c) =>
        new(c.Id, c.FullName, c.Email, c.PhoneNumber, c.Role, c.IsLocked, c.CreatedAt, c.LockReason);
}
