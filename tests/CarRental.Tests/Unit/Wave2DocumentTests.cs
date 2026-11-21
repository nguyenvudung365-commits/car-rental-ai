using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Customers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CarRental.Tests.Unit;

internal static class W2
{
    public static IFormFile File() => new Mock<IFormFile>().Object;

    public static Mock<IUnitOfWork> Uow()
    {
        var m = new Mock<IUnitOfWork>();
        m.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<int>> f, CancellationToken _) => f());
        return m;
    }

    public static Mock<ICurrentUser> Actor(int? id, string role)
    {
        var m = new Mock<ICurrentUser>();
        m.SetupGet(a => a.UserId).Returns(id);
        m.SetupGet(a => a.Role).Returns(role);
        m.SetupGet(a => a.IsAdmin).Returns(role == "Admin");
        m.SetupGet(a => a.IsStaffOrAdmin).Returns(role is "Admin" or "Staff");
        return m;
    }

    public static CustomerDocument Doc(int id = 1, int customerId = 5, DocumentType type = DocumentType.Cccd,
        DocumentStatus status = DocumentStatus.ChoDuyet, DateTime? expiry = null, string? back = null) => new()
    {
        Id = id, CustomerId = customerId, Type = type, Status = status, Number = "012345678901", FrontKey = "private/documents/5/f.webp",
        BackKey = back, ExpiryDate = expiry
    };
}

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _docs = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IFileStorageService> _files = new();
    private readonly Mock<IUnitOfWork> _uow = W2.Uow();

    private DocumentService Svc(int? actorId = 99, string role = "Staff")
        => new(_docs.Object, _customers.Object, _files.Object, _uow.Object, W2.Actor(actorId, role).Object);

    private static SubmitDocumentRequest Req(DocumentType type = DocumentType.Cccd, string number = "012345678901",
        DateTime? expiry = null, bool back = false)
        => new(type, number, expiry, "B2", W2.File(), back ? W2.File() : null);

    [Theory]
    [InlineData("12345678901")]
    [InlineData("1234567890123")]
    [InlineData("abcdefghijkl")]
    public async Task Submit_Number_Not12Digits_400(string number)
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Svc().SubmitAsync(5, Req(number: number)));
        Assert.Equal("INVALID_DOCUMENT_NUMBER", ex.Code);
        _files.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Submit_UndefinedType_400() =>
        Assert.Equal("INVALID_DOCUMENT_TYPE", (await Assert.ThrowsAsync<BadRequestException>(() => Svc().SubmitAsync(5, Req((DocumentType)9)))).Code);

    [Fact]
    public async Task Submit_License_NoExpiry_400() =>
        Assert.Equal("INVALID_EXPIRY", (await Assert.ThrowsAsync<BadRequestException>(() => Svc().SubmitAsync(5, Req(DocumentType.BangLai)))).Code);

    [Fact]
    public async Task Submit_License_ExpiryTodayOrPast_400()
    {
        foreach (var d in new[] { DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(-1) })
            Assert.Equal("INVALID_EXPIRY", (await Assert.ThrowsAsync<BadRequestException>(
                () => Svc().SubmitAsync(5, Req(DocumentType.BangLai, expiry: d)))).Code);
    }

    [Fact]
    public async Task Submit_Cccd_NormalizesNumber_StoresInPrivateFolder_NoBack_IgnoresExpiry()
    {
        _files.Setup(f => f.SaveAsync(It.IsAny<IFormFile>(), "private/documents/5")).ReturnsAsync("private/documents/5/a.webp");
        var dto = await Svc().SubmitAsync(5, Req(number: "0123 4567 8901", expiry: DateTime.UtcNow.AddYears(1)));
        Assert.Equal("012345678901", dto.Number);
        Assert.Equal(DocumentStatus.ChoDuyet, dto.Status);
        Assert.False(dto.HasBack);
        Assert.Null(dto.ExpiryDate);
        _docs.Verify(d => d.AddAsync(It.Is<CustomerDocument>(x => x.FrontKey == "private/documents/5/a.webp" && x.BackKey == null)), Times.Once);
    }

    [Fact]
    public async Task Submit_License_WithBack_Ok()
    {
        _files.Setup(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>())).ReturnsAsync("private/documents/5/k.webp");
        var dto = await Svc().SubmitAsync(5, Req(DocumentType.BangLai, expiry: DateTime.UtcNow.AddYears(2), back: true));
        Assert.True(dto.HasBack);
        Assert.Equal("B2", dto.LicenseClass);
    }

    [Fact]
    public async Task Submit_Again_SoftDeletesOld_InTransaction()
    {
        var old = W2.Doc(status: DocumentStatus.DaDuyet);
        _docs.Setup(d => d.GetActiveAsync(5, DocumentType.Cccd)).ReturnsAsync(old);
        _files.Setup(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>())).ReturnsAsync("private/documents/5/n.webp");
        await Svc().SubmitAsync(5, Req());
        Assert.True(old.IsDeleted);
        _docs.Verify(d => d.AddAsync(It.IsAny<CustomerDocument>()), Times.Once);
        _uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<int>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_Cccd_SyncsCustomerIdentityNumber_Stamps()
    {
        var doc = W2.Doc();
        var customer = new Customer { Id = 5, IdentityNumber = "999" };
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(doc);
        _customers.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(customer);
        var dto = await Svc(99).ApproveAsync(1);
        Assert.Equal(DocumentStatus.DaDuyet, dto.Status);
        Assert.Equal("012345678901", customer.IdentityNumber);
        Assert.Equal(99, doc.ReviewedById);
        Assert.NotNull(doc.ReviewedAt);
    }

    [Fact]
    public async Task Approve_License_DoesNotTouchCustomer()
    {
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(W2.Doc(type: DocumentType.BangLai, expiry: DateTime.UtcNow.AddYears(1)));
        await Svc().ApproveAsync(1);
        _customers.Verify(c => c.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("Admin")]
    public async Task Review_OwnDocument_Forbidden_SelfReview(string role)
    {
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(W2.Doc(customerId: 77));
        var s = Svc(77, role);
        Assert.Equal("SELF_REVIEW", (await Assert.ThrowsAsync<ForbiddenException>(() => s.ApproveAsync(1))).Code);
        Assert.Equal("SELF_REVIEW", (await Assert.ThrowsAsync<ForbiddenException>(() => s.RejectAsync(1, new("lý do")))).Code);
    }

    [Fact]
    public async Task Review_AlreadyProcessed_409()
    {
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(W2.Doc(status: DocumentStatus.DaDuyet));
        await Assert.ThrowsAsync<ConflictException>(() => Svc().ApproveAsync(1));
        await Assert.ThrowsAsync<ConflictException>(() => Svc().RejectAsync(1, new("x")));
    }

    [Fact]
    public async Task Review_NotFound_404() => await Assert.ThrowsAsync<NotFoundException>(() => Svc().ApproveAsync(404));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reject_NoReason_400_DocUntouched(string? reason)
    {
        var doc = W2.Doc();
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(doc);
        Assert.Equal("REASON_REQUIRED", (await Assert.ThrowsAsync<BadRequestException>(() => Svc().RejectAsync(1, new(reason)))).Code);
        Assert.Equal(DocumentStatus.ChoDuyet, doc.Status);
    }

    [Fact]
    public async Task Reject_WithReason_SetsStatusAndTrimmedReason()
    {
        var doc = W2.Doc();
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(doc);
        var dto = await Svc().RejectAsync(1, new("  mờ  "));
        Assert.Equal(DocumentStatus.TuChoi, dto.Status);
        Assert.Equal("mờ", doc.RejectReason);
        Assert.Equal(99, doc.ReviewedById);
    }

    [Theory]
    [InlineData(5, "Customer", true)]
    [InlineData(99, "Staff", true)]
    [InlineData(98, "Admin", true)]
    [InlineData(6, "Customer", false)]
    public async Task GetFile_Permissions(int actorId, string role, bool allowed)
    {
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(W2.Doc(customerId: 5, back: "private/documents/5/b.webp"));
        _files.Setup(f => f.OpenReadAsync(It.IsAny<string>())).ReturnsAsync((new MemoryStream(), "image/webp"));
        var s = Svc(actorId, role);
        if (allowed) Assert.Equal("image/webp", (await s.GetFileAsync(1, "front")).ContentType);
        else
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => s.GetFileAsync(1, "front"));
            _files.Verify(f => f.OpenReadAsync(It.IsAny<string>()), Times.Never);
        }
    }

    [Fact]
    public async Task GetFile_SideRules()
    {
        _docs.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(W2.Doc(back: null));
        _files.Setup(f => f.OpenReadAsync(It.IsAny<string>())).ReturnsAsync((new MemoryStream(), "image/webp"));
        var s = Svc(5, "Customer");
        await Assert.ThrowsAsync<NotFoundException>(() => s.GetFileAsync(1, "back"));
        Assert.Equal("INVALID_SIDE", (await Assert.ThrowsAsync<BadRequestException>(() => s.GetFileAsync(1, "side"))).Code);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetFileAsync(404, "front"));
        _files.Verify(f => f.OpenReadAsync(It.IsAny<string>()), Times.Never);
        await s.GetFileAsync(1, "FRONT");
        _files.Verify(f => f.OpenReadAsync("private/documents/5/f.webp"), Times.Once);
    }

    [Fact]
    public async Task Dto_NeverExposesStorageKeys()
    {
        _docs.Setup(d => d.GetByCustomerAsync(5)).ReturnsAsync([W2.Doc(back: "private/documents/5/b.webp")]);
        var json = System.Text.Json.JsonSerializer.Serialize(await Svc().GetMineAsync(5));
        Assert.DoesNotContain("private/", json);
    }

    [Fact]
    public async Task IsVerified_Matrix()
    {
        var end = DateTime.UtcNow.Date.AddDays(5);
        async Task<bool> Check(params CustomerDocument[] d)
        {
            _docs.Setup(x => x.GetByCustomerAsync(5)).ReturnsAsync(d.ToList());
            return await Svc().IsVerifiedForRentalAsync(5, end);
        }
        var cccd = W2.Doc(status: DocumentStatus.DaDuyet);
        CustomerDocument Lic(DocumentStatus s, int off) => W2.Doc(2, type: DocumentType.BangLai, status: s, expiry: end.AddDays(off));

        Assert.True(await Check(cccd, Lic(DocumentStatus.DaDuyet, 0)));
        Assert.True(await Check(cccd, Lic(DocumentStatus.DaDuyet, 30)));
        Assert.False(await Check(cccd, Lic(DocumentStatus.DaDuyet, -1)));
        Assert.False(await Check(cccd, Lic(DocumentStatus.ChoDuyet, 30)));
        Assert.False(await Check(cccd, Lic(DocumentStatus.TuChoi, 30)));
        Assert.False(await Check(cccd));
        Assert.False(await Check(Lic(DocumentStatus.DaDuyet, 30)));
        Assert.False(await Check(W2.Doc(status: DocumentStatus.ChoDuyet), Lic(DocumentStatus.DaDuyet, 30)));
        Assert.False(await Check());
    }

    [Fact]
    public void Overall_Rules()
    {
        CustomerDocument D(DocumentType t, DocumentStatus s) => W2.Doc(type: t, status: s);
        Assert.Equal(DocumentsOverallStatus.ChuaNop, DocumentService.Overall([]));
        Assert.Equal(DocumentsOverallStatus.DaDuyet, DocumentService.Overall([D(DocumentType.Cccd, DocumentStatus.DaDuyet), D(DocumentType.BangLai, DocumentStatus.DaDuyet)]));
        Assert.Equal(DocumentsOverallStatus.ChoDuyet, DocumentService.Overall([D(DocumentType.Cccd, DocumentStatus.DaDuyet), D(DocumentType.BangLai, DocumentStatus.ChoDuyet)]));
        Assert.Equal(DocumentsOverallStatus.TuChoi, DocumentService.Overall([D(DocumentType.Cccd, DocumentStatus.DaDuyet), D(DocumentType.BangLai, DocumentStatus.TuChoi)]));
        Assert.Equal(DocumentsOverallStatus.ChoDuyet, DocumentService.Overall([D(DocumentType.Cccd, DocumentStatus.ChoDuyet)]));
    }
}
