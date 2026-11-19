using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;
using Moq;

namespace CarRental.Tests.Unit;

public class BookingServiceGuardTests
{
    private readonly Mock<IBookingService> _inner = new();
    private readonly Mock<ICustomerScreening> _screen = new();
    private static readonly CreateBookingRequest Req = new(1, DateTime.UtcNow.Date.AddDays(3), DateTime.UtcNow.Date.AddDays(5), null);

    private BookingServiceGuard Guard() => new(_inner.Object, _screen.Object);

    private static BookingDto Dto(int customerId = 5) => new(9, customerId, "n", 1, "c", "p", DateTime.UtcNow, DateTime.UtcNow, 1,
        null, 1, 1, false, "ChoXacNhan", null, null, DateTime.UtcNow);

    [Fact]
    public async Task Create_Chan_ThrowsForbidden_InnerNotCalled_MessageHidesReason()
    {
        _screen.Setup(s => s.ScreenAsync(5)).ReturnsAsync(new ScreeningResult(BlacklistLevel.Chan, "BÍ MẬT-LÝ-DO"));
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => Guard().CreateAsync(5, Req));
        Assert.Equal("CUSTOMER_BLOCKED", ex.Code);
        Assert.DoesNotContain("BÍ MẬT-LÝ-DO", ex.Message);
        _inner.Verify(i => i.CreateAsync(It.IsAny<int>(), It.IsAny<CreateBookingRequest>()), Times.Never);
    }

    [Theory]
    [InlineData(BlacklistLevel.CanhBao)]
    [InlineData(null)]
    public async Task Create_CanhBaoOrNone_CallsInner(BlacklistLevel? level)
    {
        _screen.Setup(s => s.ScreenAsync(5)).ReturnsAsync(new ScreeningResult(level, null));
        _inner.Setup(i => i.CreateAsync(5, Req)).ReturnsAsync(Dto());
        Assert.NotNull(await Guard().CreateAsync(5, Req));
        _inner.Verify(i => i.CreateAsync(5, Req), Times.Once);
    }

    [Fact]
    public async Task Confirm_Chan_Conflict_InnerConfirmNotCalled()
    {
        _inner.Setup(i => i.GetByIdAsync(9, null)).ReturnsAsync(Dto(5));
        _screen.Setup(s => s.ScreenAsync(5)).ReturnsAsync(new ScreeningResult(BlacklistLevel.Chan, "x"));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Guard().ConfirmAsync(9));
        Assert.Equal("CUSTOMER_BLOCKED", ex.Code);
        _inner.Verify(i => i.ConfirmAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Confirm_CanhBao_CallsInner()
    {
        _inner.Setup(i => i.GetByIdAsync(9, null)).ReturnsAsync(Dto(5));
        _inner.Setup(i => i.ConfirmAsync(9)).ReturnsAsync(Dto(5));
        _screen.Setup(s => s.ScreenAsync(5)).ReturnsAsync(new ScreeningResult(BlacklistLevel.CanhBao, "x"));
        await Guard().ConfirmAsync(9);
        _inner.Verify(i => i.ConfirmAsync(9), Times.Once);
    }

    [Fact]
    public async Task OtherMethods_PassThrough_WithoutScreening()
    {
        _inner.Setup(i => i.CancelAsync(9, "r")).ReturnsAsync(Dto());
        await Guard().CancelAsync(9, "r");
        await Guard().GetBookedRangesAsync(1);
        _inner.Verify(i => i.CancelAsync(9, "r"), Times.Once);
        _screen.Verify(s => s.ScreenAsync(It.IsAny<int>()), Times.Never);
    }
}

public class BlacklistServiceTests
{
    private readonly Mock<IBlacklistRepository> _repo = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _me = new();

    public BlacklistServiceTests() => _me.SetupGet(m => m.UserId).Returns(42);

    private BlacklistService Svc() => new(_repo.Object, _customers.Object, _uow.Object, _me.Object);

    [Fact]
    public async Task Create_NoKey_BadRequest()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc().CreateAsync(new CreateBlacklistRequest(null, "  ", null, BlacklistLevel.Chan, "lý do")));
        Assert.Equal("BLACKLIST_KEY_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Create_UndefinedLevel_BadRequest()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc().CreateAsync(new CreateBlacklistRequest(null, "012345678901", null, (BlacklistLevel)0, "lý do")));
        Assert.Equal("INVALID_LEVEL", ex.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Create_NoReason_BadRequest(string? reason)
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc().CreateAsync(new CreateBlacklistRequest(null, "012345678901", null, BlacklistLevel.Chan, reason)));
        Assert.Equal("REASON_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Create_UnknownCustomer_NotFound()
    {
        _customers.Setup(c => c.GetByIdAsync(77)).ReturnsAsync((Customer?)null);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Svc().CreateAsync(new CreateBlacklistRequest(77, null, null, BlacklistLevel.Chan, "x")));
    }

    [Fact]
    public async Task Create_Ok_RecordsCreatorAndTrimsKeys()
    {
        BlacklistEntry? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<BlacklistEntry>())).Callback<BlacklistEntry>(e => saved = e).Returns(Task.CompletedTask);
        var dto = await Svc().CreateAsync(new CreateBlacklistRequest(null, " 0123 ", null, BlacklistLevel.CanhBao, " r "));
        Assert.Equal(42, saved!.CreatedById);
        Assert.Equal("0123", saved.IdentityNumber);
        Assert.Equal("r", saved.Reason);
        Assert.Null(saved.RemovedAt);
        Assert.Equal(BlacklistLevel.CanhBao, dto.Level);
    }

    [Fact]
    public async Task Create_WithCustomer_FillsMissingIdentityAndPhoneFromCustomer()
    {
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5, FullName = "A", IdentityNumber = "012 345 678 901", PhoneNumber = "+84 912 345 678" });
        BlacklistEntry? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<BlacklistEntry>())).Callback<BlacklistEntry>(e => saved = e).Returns(Task.CompletedTask);
        await Svc().CreateAsync(new CreateBlacklistRequest(5, null, " ", BlacklistLevel.Chan, "r"));
        Assert.Equal("012345678901", saved!.IdentityNumber);
        Assert.Equal("0912345678", saved.PhoneNumber);
    }

    [Fact]
    public async Task Create_WithCustomer_KeepsExplicitKeys()
    {
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5, IdentityNumber = "111111111111", PhoneNumber = "0900000000" });
        BlacklistEntry? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<BlacklistEntry>())).Callback<BlacklistEntry>(e => saved = e).Returns(Task.CompletedTask);
        await Svc().CreateAsync(new CreateBlacklistRequest(5, "222222222222", "0911111111", BlacklistLevel.Chan, "r"));
        Assert.Equal("222222222222", saved!.IdentityNumber);
        Assert.Equal("0911111111", saved.PhoneNumber);
    }

    [Fact]
    public async Task Remove_NoReason_BadRequest_EntryUntouched()
    {
        var e = new BlacklistEntry { Id = 3, Reason = "r" };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(e);
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().RemoveAsync(3, new RemoveBlacklistRequest("  ")));
        Assert.Equal("REASON_REQUIRED", ex.Code);
        Assert.Null(e.RemovedAt);
    }

    [Fact]
    public async Task Remove_OnlySetsRemovedFields_NoDelete()
    {
        var e = new BlacklistEntry { Id = 3, Level = BlacklistLevel.Chan, Reason = "r", IdentityNumber = "1", CreatedById = 1 };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(e);
        await Svc().RemoveAsync(3, new RemoveBlacklistRequest("hết hạn"));
        Assert.NotNull(e.RemovedAt);
        Assert.Equal(42, e.RemovedById);
        Assert.Equal("hết hạn", e.RemoveReason);
        Assert.False(e.IsDeleted);
        Assert.Equal("r", e.Reason);
        Assert.Equal(BlacklistLevel.Chan, e.Level);
    }

    [Fact]
    public async Task Remove_Twice_AlreadyRemoved()
    {
        var e = new BlacklistEntry { Id = 3, RemovedAt = DateTime.UtcNow };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(e);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Svc().RemoveAsync(3, new RemoveBlacklistRequest(null)));
        Assert.Equal("ALREADY_REMOVED", ex.Code);
    }

    [Fact]
    public async Task Remove_NotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().RemoveAsync(1, new RemoveBlacklistRequest(null)));

    [Fact]
    public async Task Screen_HighestLevelWins()
    {
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5, IdentityNumber = "111", PhoneNumber = "090" });
        _repo.Setup(r => r.FindActiveMatchesAsync(5, "111", "090")).ReturnsAsync(
        [
            new BlacklistEntry { Level = BlacklistLevel.Chan, Reason = "chan" },
            new BlacklistEntry { Level = BlacklistLevel.CanhBao, Reason = "canhbao", CreatedAt = DateTime.UtcNow.AddDays(1) }
        ]);
        var r = await Svc().ScreenAsync(5);
        Assert.Equal(BlacklistLevel.Chan, r.Level);
        Assert.Equal("chan", r.Reason);
    }

    [Fact]
    public async Task Screen_NoMatchOrUnknownCustomer_NullLevel()
    {
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(new Customer { Id = 5 });
        _repo.Setup(r => r.FindActiveMatchesAsync(5, null, It.IsAny<string?>())).ReturnsAsync([]);
        Assert.Null((await Svc().ScreenAsync(5)).Level);
        Assert.Null((await Svc().ScreenAsync(404)).Level);
    }
}

public class IdentityNormalizerTests
{
    [Theory]
    [InlineData("0912 345 678", "0912345678")]
    [InlineData("+84 912-345-678", "0912345678")]
    [InlineData("84912345678", "0912345678")]
    [InlineData("0912345678", "0912345678")]
    [InlineData("abc", null)]
    [InlineData(null, null)]
    public void Phone_Normalizes(string? input, string? expected) => Assert.Equal(expected, IdentityNormalizer.Phone(input));

    [Theory]
    [InlineData(" 012 345-678 901 ", "012345678901")]
    [InlineData("", null)]
    public void Cccd_KeepsDigitsOnly(string? input, string? expected) => Assert.Equal(expected, IdentityNormalizer.Cccd(input));
}
