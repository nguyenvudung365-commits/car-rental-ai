using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CarRental.Tests.Unit;

public class LoyaltyServiceTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ILoyaltyRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Customer _c = new() { Id = 5 };

    public LoyaltyServiceTests()
    {
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(_c);
        _repo.Setup(r => r.GetRecentAsync(5, It.IsAny<int>())).ReturnsAsync([]);
    }

    private LoyaltyService Svc() => new(_customers.Object, _repo.Object, _uow.Object);

    [Fact]
    public async Task Award_AddsOnePercent_UpdatesBalanceAndLifetime_RecordsTransaction()
    {
        await Svc().AwardForBookingAsync(5, 9, 2_100_000m);
        Assert.Equal(21_000, _c.LoyaltyPoints);
        Assert.Equal(21_000, _c.LifetimePoints);
        _repo.Verify(r => r.AddAsync(It.Is<LoyaltyTransaction>(t => t.BookingId == 9 && t.Points == 21_000 && t.Reason == LoyaltyReason.TichDiem && t.CustomerId == 5)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Award_AlreadyAwarded_Skipped()
    {
        _repo.Setup(r => r.ExistsAsync(9, LoyaltyReason.TichDiem)).ReturnsAsync(true);
        await Svc().AwardForBookingAsync(5, 9, 2_100_000m);
        Assert.Equal(0, _c.LoyaltyPoints);
        _repo.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Award_ZeroPoints_NoTransaction()
    {
        await Svc().AwardForBookingAsync(5, 9, 99m);
        _repo.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Get_ComputesTierAndNext()
    {
        _c.LoyaltyPoints = 50; _c.LifetimePoints = 120_000;
        var dto = await Svc().GetAsync(5);
        Assert.Equal(MemberTier.Bac, dto.Tier);
        Assert.Equal(MemberTier.Vang, dto.NextTier);
        Assert.Equal(180_000, dto.PointsToNextTier);
    }

    [Theory]
    [InlineData(0, "ghi chú", "INVALID_POINTS")]
    [InlineData(10, null, "REASON_REQUIRED")]
    [InlineData(10, "  ", "REASON_REQUIRED")]
    public async Task Adjust_Validation(int pts, string? note, string code)
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().AdjustAsync(5, new(pts, note)));
        Assert.Equal(code, ex.Code);
        _repo.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Adjust_CannotGoNegative_ButExactZeroOk()
    {
        _c.LoyaltyPoints = 100; _c.LifetimePoints = 100;
        Assert.Equal("INSUFFICIENT_POINTS", (await Assert.ThrowsAsync<BadRequestException>(() => Svc().AdjustAsync(5, new(-101, "x")))).Code);
        Assert.Equal(100, _c.LoyaltyPoints);
        await Svc().AdjustAsync(5, new(-100, "x"));
        Assert.Equal(0, _c.LoyaltyPoints);
    }

    [Fact]
    public async Task Adjust_Decrease_KeepsLifetime_Increase_RaisesLifetime()
    {
        _c.LoyaltyPoints = 500; _c.LifetimePoints = 500;
        await Svc().AdjustAsync(5, new(-200, "đổi quà"));
        Assert.Equal(300, _c.LoyaltyPoints);
        Assert.Equal(500, _c.LifetimePoints);
        await Svc().AdjustAsync(5, new(50, "  thưởng "));
        Assert.Equal(550, _c.LifetimePoints);
        _repo.Verify(r => r.AddAsync(It.Is<LoyaltyTransaction>(t => t.Reason == LoyaltyReason.DieuChinh && t.BookingId == null && t.Note == "thưởng")), Times.Once);
    }

    [Fact]
    public async Task Adjust_UnknownCustomer_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().AdjustAsync(404, new(5, "x")));
}

public class CustomerAdminServiceTests
{
    private readonly Mock<ICustomerAdminRepository> _admin = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IDocumentRepository> _docs = new();
    private readonly Mock<ILoyaltyService> _loyalty = new();
    private readonly Mock<ICustomerScreening> _screen = new();

    private CustomerAdminService Svc(string role) => new(_admin.Object, _customers.Object, _docs.Object, _loyalty.Object, _screen.Object, W2.Actor(1, role).Object);

    private void SetupDetail()
    {
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(new Customer
        {
            Id = 5, FullName = "A", Email = "a@t.vn", PhoneNumber = "0900", IdentityNumber = "012345678901", Address = "HCM",
            IsLocked = true, LockReason = "gian lận"
        });
        _docs.Setup(d => d.GetByCustomerAsync(5)).ReturnsAsync([]);
        _admin.Setup(a => a.GetStatsAsync(5)).ReturnsAsync((2, 1_000m));
        _admin.Setup(a => a.GetRecentBookingsAsync(5, 10)).ReturnsAsync([]);
        _screen.Setup(s => s.ScreenAsync(5)).ReturnsAsync(new ScreeningResult(null, null));
        _loyalty.Setup(l => l.GetAsync(5)).ReturnsAsync(new LoyaltyDto(10, 10, MemberTier.ThanhVien, MemberTier.Bac, 99_990,
            [new LoyaltyTransactionDto(1, null, 10, LoyaltyReason.DieuChinh, "x", DateTime.UtcNow)]));
    }

    [Fact]
    public async Task Get_Admin_SeesEverything()
    {
        SetupDetail();
        var d = await Svc("Admin").GetAsync(5);
        Assert.Equal("012345678901", d.IdentityNumber);
        Assert.Equal("HCM", d.Address);
        Assert.Equal("gian lận", d.LockReason);
        Assert.Single(d.Loyalty.History);
    }

    [Fact]
    public async Task Get_Staff_GetsRedactedView()
    {
        SetupDetail();
        _docs.Setup(x => x.GetByCustomerAsync(5)).ReturnsAsync([new CustomerDocument { Id = 1, CustomerId = 5, Number = "012345678901" }]);
        var d = await Svc("Staff").GetAsync(5);
        Assert.Equal("********8901", d.Documents[0].Number);
        Assert.Null(d.IdentityNumber);
        Assert.Null(d.Address);
        Assert.Null(d.LockReason);
        Assert.Empty(d.Loyalty.History);
        Assert.True(d.IsLocked);
        Assert.Equal(2, d.BookingCount);
        Assert.Equal(10, d.Loyalty.Points);
    }

    [Fact]
    public async Task Get_Unknown_404() => await Assert.ThrowsAsync<NotFoundException>(() => Svc("Admin").GetAsync(404));

    [Fact]
    public async Task List_TrimsSearch_ClampsPaging_MapsTier()
    {
        CustomerListFilterRequest? seen = null; int p = 0, ps = 0;
        _admin.Setup(a => a.SearchAsync(It.IsAny<CustomerListFilterRequest>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback((CustomerListFilterRequest f, int page, int size) => { seen = f; p = page; ps = size; })
            .ReturnsAsync(([new CustomerListRow(new Customer { Id = 1, LifetimePoints = 300_000, FullName = "x" }, DocumentsOverallStatus.ChuaNop, null, 0, 0m)], 1));
        var (items, total) = await Svc("Staff").ListAsync(new("  an  ", null, null, null, Page: 0, PageSize: 5000));
        Assert.Equal("an", seen!.Search);
        Assert.Equal((1, 100), (p, ps));
        Assert.Equal(MemberTier.Vang, items[0].Tier);
        Assert.Equal(1, total);
    }
}

public class CustomerServiceWave2Tests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IDocumentRepository> _docs = new();
    private readonly Mock<IFileStorageService> _files = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Customer _c = new() { Id = 5, PasswordHash = "OLDHASH", IdentityNumber = "012345678901" };

    public CustomerServiceWave2Tests()
    {
        _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(_c);
        _hasher.Setup(h => h.Verify("OldPass1!", "OLDHASH")).Returns(true);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(s => "H:" + s);
    }

    private CustomerService Svc() => new(_repo.Object, _hasher.Object, new Mock<IJwtTokenGenerator>().Object, _uow.Object, _docs.Object, _files.Object);

    [Fact]
    public async Task ChangePassword_WrongCurrent_400_HashUnchanged()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().ChangePasswordAsync(5, new("nope", "NewPass12")));
        Assert.Equal("WRONG_PASSWORD", ex.Code);
        Assert.Equal("OLDHASH", _c.PasswordHash);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_SameAsOld_400()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().ChangePasswordAsync(5, new("OldPass1!", "OldPass1!")));
        Assert.Equal("SAME_PASSWORD", ex.Code);
    }

    [Fact]
    public async Task ChangePassword_Ok_HashChanged_Saved()
    {
        await Svc().ChangePasswordAsync(5, new("OldPass1!", "NewPass12"));
        Assert.Equal("H:NewPass12", _c.PasswordHash);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatar_SetsKey_DeletesOld_IgnoresDeleteFailure()
    {
        _c.AvatarKey = "avatars/5/old.webp";
        _files.Setup(f => f.SaveAsync(It.IsAny<IFormFile>(), "avatars/5")).ReturnsAsync("avatars/5/new.webp");
        _files.Setup(f => f.Delete("avatars/5/old.webp")).Throws(new IOException("boom"));
        var dto = await Svc().UpdateAvatarAsync(5, W2.File());
        Assert.Equal("avatars/5/new.webp", dto.AvatarKey);
        Assert.Equal("avatars/5/new.webp", _c.AvatarKey);
        _files.Verify(f => f.Delete("avatars/5/old.webp"), Times.Once);
    }

    [Fact]
    public async Task UpdateAvatar_SaveFails_KeyUntouched_OldNotDeleted()
    {
        _c.AvatarKey = "avatars/5/old.webp";
        _files.Setup(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("bad webp"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Svc().UpdateAvatarAsync(5, W2.File()));
        Assert.Equal("avatars/5/old.webp", _c.AvatarKey);
        _files.Verify(f => f.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_ChangeCccd_ApprovedGoesBackToPending()
    {
        var doc = W2.Doc(status: DocumentStatus.DaDuyet); doc.ReviewedById = 1; doc.ReviewedAt = DateTime.UtcNow;
        _docs.Setup(d => d.GetActiveAsync(5, DocumentType.Cccd)).ReturnsAsync(doc);
        await Svc().UpdateProfileAsync(5, new("A", "999988887777", "HCM"));
        Assert.Equal(DocumentStatus.ChoDuyet, doc.Status);
        Assert.Null(doc.ReviewedById);
        Assert.Null(doc.ReviewedAt);
        Assert.Equal("999988887777", _c.IdentityNumber);
    }

    [Fact]
    public async Task UpdateProfile_SameCccdDifferentFormatting_KeepsApproved()
    {
        var doc = W2.Doc(status: DocumentStatus.DaDuyet);
        _docs.Setup(d => d.GetActiveAsync(5, DocumentType.Cccd)).ReturnsAsync(doc);
        await Svc().UpdateProfileAsync(5, new("A", "0123 4567 8901", "HCM"));
        Assert.Equal(DocumentStatus.DaDuyet, doc.Status);
    }

    [Fact]
    public async Task UpdateProfile_ChangeCccd_RejectedOrNone_NotTouched()
    {
        var rejected = W2.Doc(status: DocumentStatus.TuChoi);
        _docs.Setup(d => d.GetActiveAsync(5, DocumentType.Cccd)).ReturnsAsync(rejected);
        await Svc().UpdateProfileAsync(5, new("A", "999988887777", null));
        Assert.Equal(DocumentStatus.TuChoi, rejected.Status);
        _docs.Setup(d => d.GetActiveAsync(5, DocumentType.Cccd)).ReturnsAsync((CustomerDocument?)null);
        await Svc().UpdateProfileAsync(5, new("A", "111122223333", null));
        Assert.Equal("111122223333", _c.IdentityNumber);
    }

    [Fact]
    public async Task UpdateProfile_OnlyNameChanged_DoesNotQueryDocuments()
    {
        await Svc().UpdateProfileAsync(5, new("Tên mới", "012345678901", null));
        _docs.Verify(d => d.GetActiveAsync(It.IsAny<int>(), It.IsAny<DocumentType>()), Times.Never);
    }

    [Fact]
    public async Task GetProfile_Maps()
    {
        _c.LifetimePoints = 800_000; _c.LoyaltyPoints = 1;
        _docs.Setup(d => d.GetByCustomerAsync(5)).ReturnsAsync([]);
        var p = await Svc().GetProfileAsync(5);
        Assert.Equal(MemberTier.KimCuong, p.Tier);
        Assert.Equal(DocumentsOverallStatus.ChuaNop, p.DocumentsStatus);
        Assert.Equal("Customer", p.Role);
    }
}
