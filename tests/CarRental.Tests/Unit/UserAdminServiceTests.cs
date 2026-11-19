using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;
using Moq;

namespace CarRental.Tests.Unit;

public class UserAdminServiceTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _me = new();

    public UserAdminServiceTests() => _me.SetupGet(m => m.UserId).Returns(1);

    private UserAdminService Svc()
    {
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => "H:" + p);
        return new(_repo.Object, hasher.Object, _uow.Object, _me.Object);
    }

    private Customer Add(int id, UserRole role, bool locked = false)
    {
        var c = new Customer { Id = id, Role = role, IsLocked = locked, FullName = "u" + id, Email = $"u{id}@t.vn", PhoneNumber = "09" + id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(c);
        return c;
    }

    [Fact]
    public async Task ChangeRole_LastAdminDemoted_LastAdmin()
    {
        Add(2, UserRole.Admin);
        _repo.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(1);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().ChangeRoleAsync(2, new ChangeRoleRequest(UserRole.Staff)));
        Assert.Equal("LAST_ADMIN", ex.Code);
    }

    [Fact]
    public async Task ChangeRole_AdminDemoted_WhenAnotherAdminExists_Passes()
    {
        var u = Add(2, UserRole.Admin);
        _repo.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(2);
        await Svc().ChangeRoleAsync(2, new ChangeRoleRequest(UserRole.Staff));
        Assert.Equal(UserRole.Staff, u.Role);
    }

    [Fact]
    public async Task ChangeRole_Self_SelfChange()
    {
        Add(1, UserRole.Admin);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().ChangeRoleAsync(1, new ChangeRoleRequest(UserRole.Staff)));
        Assert.Equal("SELF_CHANGE", ex.Code);
    }

    [Fact]
    public async Task ChangeRole_UndefinedRole_BadRequest()
    {
        Add(2, UserRole.Staff);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().ChangeRoleAsync(2, new ChangeRoleRequest((UserRole)99)));
        Assert.Equal("INVALID_ROLE", ex.Code);
    }

    [Fact]
    public async Task ChangeRole_NotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().ChangeRoleAsync(404, new ChangeRoleRequest(UserRole.Staff)));

    [Fact]
    public async Task Lock_Self_SelfLock()
    {
        Add(1, UserRole.Admin);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().LockAsync(1, new LockUserRequest("x")));
        Assert.Equal("SELF_LOCK", ex.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Lock_NoReason_ReasonRequired(string? reason)
    {
        Add(2, UserRole.Customer);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().LockAsync(2, new LockUserRequest(reason)));
        Assert.Equal("REASON_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Lock_LastAdmin_LastAdmin()
    {
        Add(2, UserRole.Admin);
        _repo.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(1);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().LockAsync(2, new LockUserRequest("vi phạm")));
        Assert.Equal("LAST_ADMIN", ex.Code);
    }

    [Fact]
    public async Task Lock_Customer_SetsFields_AndUnlockClears()
    {
        var u = Add(2, UserRole.Customer);
        await Svc().LockAsync(2, new LockUserRequest("  vi phạm  "));
        Assert.True(u.IsLocked);
        Assert.Equal("vi phạm", u.LockReason);
        Assert.NotNull(u.LockedAt);
        await Svc().UnlockAsync(2);
        Assert.False(u.IsLocked);
        Assert.Null(u.LockReason);
        Assert.Null(u.LockedAt);
    }

    [Fact]
    public async Task CreateStaff_DuplicateEmail_EmailExists()
    {
        _repo.Setup(r => r.GetByEmailAsync("a@t.vn")).ReturnsAsync(new Customer());
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            Svc().CreateStaffAsync(new CreateStaffRequest("A", " A@T.vn ", "0911111111", "12345678")));
        Assert.Equal("EMAIL_EXISTS", ex.Code);
    }

    [Fact]
    public async Task CreateStaff_DuplicatePhone_PhoneExists()
    {
        _repo.Setup(r => r.GetByPhoneNumberAsync("0911111111")).ReturnsAsync(new Customer());
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            Svc().CreateStaffAsync(new CreateStaffRequest("A", "a@t.vn", "0911111111", "12345678")));
        Assert.Equal("PHONE_EXISTS", ex.Code);
    }

    [Fact]
    public async Task CreateStaff_Ok_RoleStaff_HashedPassword_NoHashInDto()
    {
        Customer? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Customer>())).Callback<Customer>(c => saved = c).Returns(Task.CompletedTask);
        var dto = await Svc().CreateStaffAsync(new CreateStaffRequest("A", "A@t.vn", "0911111111", "12345678"));
        Assert.Equal(UserRole.Staff, saved!.Role);
        Assert.Equal("H:12345678", saved.PasswordHash);
        Assert.Equal("a@t.vn", dto.Email);
        Assert.DoesNotContain(typeof(UserSummaryDto).GetProperties(), p => p.Name.Contains("Password"));
    }
}
