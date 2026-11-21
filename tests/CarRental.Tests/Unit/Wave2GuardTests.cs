using System.Reflection;
using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using Moq;

namespace CarRental.Tests.Unit;

public class RentalServiceGuardTests
{
    private readonly Mock<IRentalService> _inner = new();
    private readonly Mock<IBookingService> _bookings = new();
    private readonly Mock<IDocumentVerification> _docs = new();
    private readonly Mock<ILoyaltyService> _loyalty = new();

    private RentalServiceGuard Guard() => new(_inner.Object, _bookings.Object, _docs.Object, _loyalty.Object);

    private static BookingDto B(string status) => new(9, 5, "n", 1, "c", "p", DateTime.UtcNow, DateTime.UtcNow.Date.AddDays(3), 3, null, 1, 1, false, status, null, null, DateTime.UtcNow);

    [Fact]
    public async Task Pickup_NotVerified_409_InnerNotCalled()
    {
        _bookings.Setup(b => b.GetByIdAsync(9, null)).ReturnsAsync(B("DaXacNhan"));
        _docs.Setup(d => d.IsVerifiedForRentalAsync(5, It.IsAny<DateTime>())).ReturnsAsync(false);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Guard().PickupAsync(9));
        Assert.Equal("DOCUMENTS_NOT_VERIFIED", ex.Code);
        _inner.Verify(i => i.PickupAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Pickup_Verified_CallsInner_WithBookingEndDate()
    {
        var dto = B("DaXacNhan");
        _bookings.Setup(b => b.GetByIdAsync(9, null)).ReturnsAsync(dto);
        _docs.Setup(d => d.IsVerifiedForRentalAsync(5, dto.EndDate)).ReturnsAsync(true);
        _inner.Setup(i => i.PickupAsync(9)).ReturnsAsync(dto);
        await Guard().PickupAsync(9);
        _inner.Verify(i => i.PickupAsync(9), Times.Once);
    }

    [Theory]
    [InlineData("ChoXacNhan")]
    [InlineData("DangThue")]
    public async Task Pickup_WrongStatus_LeavesToInner_NoDocumentCheck(string status)
    {
        _bookings.Setup(b => b.GetByIdAsync(9, null)).ReturnsAsync(B(status));
        _inner.Setup(i => i.PickupAsync(9)).ReturnsAsync(B(status));
        await Guard().PickupAsync(9);
        _docs.Verify(d => d.IsVerifiedForRentalAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Return_AwardsPointsFromFinalAmount_ForBookingCustomer()
    {
        _bookings.Setup(b => b.GetByIdAsync(9, null)).ReturnsAsync(B("DangThue"));
        var rec = new ReturnRecordDto(9, DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0, null, 2_000_000, 2_100_000, 1);
        _inner.Setup(i => i.ReturnAsync(9, It.IsAny<ReturnBookingRequest>(), 1)).ReturnsAsync(rec);
        var res = await Guard().ReturnAsync(9, new(null, 0, null), 1);
        Assert.Same(rec, res);
        _loyalty.Verify(l => l.AwardForBookingAsync(5, 9, 2_100_000m), Times.Once);
    }

    [Fact]
    public async Task Return_AwardThrows_StillReturnsRecord()
    {
        _bookings.Setup(b => b.GetByIdAsync(9, null)).ReturnsAsync(B("DangThue"));
        var rec = new ReturnRecordDto(9, DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0, null, 2_000_000, 2_100_000, 1);
        _inner.Setup(i => i.ReturnAsync(9, It.IsAny<ReturnBookingRequest>(), 1)).ReturnsAsync(rec);
        _loyalty.Setup(l => l.AwardForBookingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>())).ThrowsAsync(new InvalidOperationException("db"));
        Assert.Same(rec, await Guard().ReturnAsync(9, new(null, 0, null), 1));
    }

    [Fact]
    public async Task Return_InnerFails_NoPointsAwarded()
    {
        _bookings.Setup(b => b.GetByIdAsync(9, null)).ReturnsAsync(B("DangThue"));
        _inner.Setup(i => i.ReturnAsync(9, It.IsAny<ReturnBookingRequest>(), It.IsAny<int?>())).ThrowsAsync(new ConflictException("INVALID_STATUS", "x"));
        await Assert.ThrowsAsync<ConflictException>(() => Guard().ReturnAsync(9, new(null, 0, null), 1));
        _loyalty.Verify(l => l.AwardForBookingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }
}

public class BookingServiceGuardCarVisibilityTests
{
    private readonly Mock<IBookingService> _inner = new();
    private readonly Mock<ICustomerScreening> _screen = new();
    private readonly Mock<ICarRepository> _cars = new();

    private BookingServiceGuard Guard(string role) => new(_inner.Object, _screen.Object, _cars.Object, W2.Actor(7, role).Object);

    private void CarIs(CarStatus s) => _cars.Setup(c => c.GetByIdAsync(1, false)).ReturnsAsync(new Car { Id = 1, Status = s });

    [Theory]
    [InlineData("Customer", true)]
    [InlineData("Staff", false)]
    [InlineData("Admin", false)]
    public async Task PendingCar_EstimateAndBookedRanges_HiddenFromCustomerOnly(string role, bool hidden)
    {
        CarIs(CarStatus.ChoDuyet);
        var g = Guard(role);
        var req = new EstimateBookingRequest(1, DateTime.UtcNow.Date.AddDays(2), DateTime.UtcNow.Date.AddDays(3));
        if (hidden)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => g.EstimateAsync(req));
            await Assert.ThrowsAsync<NotFoundException>(() => g.GetBookedRangesAsync(1));
            _inner.Verify(i => i.EstimateAsync(It.IsAny<EstimateBookingRequest>()), Times.Never);
            _inner.Verify(i => i.GetBookedRangesAsync(It.IsAny<int>()), Times.Never);
        }
        else
        {
            await g.EstimateAsync(req);
            await g.GetBookedRangesAsync(1);
            _inner.Verify(i => i.GetBookedRangesAsync(1), Times.Once);
        }
    }

    [Fact]
    public async Task ApprovedCar_Customer_Passes()
    {
        CarIs(CarStatus.SanSang);
        await Guard("Customer").GetBookedRangesAsync(1);
        _inner.Verify(i => i.GetBookedRangesAsync(1), Times.Once);
    }

    [Fact]
    public async Task MissingCar_PassesToInner_LetsInnerReport404()
    {
        await Guard("Customer").GetBookedRangesAsync(1);
        _inner.Verify(i => i.GetBookedRangesAsync(1), Times.Once);
    }
}

public class ModuleBoundaryWave2Tests
{
    // Chiều ngược lại: Customers không được phụ thuộc Bookings (số liệu đơn đi qua ICustomerAdminRepository ở Infrastructure).
    [Fact]
    public void CustomersModule_DoesNotReference_BookingsModule()
    {
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var bad = new List<string>();
        foreach (var t in typeof(CustomerService).Assembly.GetTypes().Where(t => t.Namespace == "CarRental.Application.Modules.Customers"))
        {
            var used = new List<Type>();
            used.AddRange(t.GetFields(flags).Select(f => f.FieldType));
            used.AddRange(t.GetProperties(flags).Select(p => p.PropertyType));
            used.AddRange(t.GetConstructors(flags).SelectMany(c => c.GetParameters().Select(p => p.ParameterType)));
            foreach (var m in t.GetMethods(flags))
            {
                used.Add(m.ReturnType);
                used.AddRange(m.GetParameters().Select(p => p.ParameterType));
            }
            foreach (var u in used.SelectMany(Flatten))
                if (u.Namespace == "CarRental.Application.Modules.Bookings") bad.Add($"{t.Name} -> {u.Name}");
        }
        Assert.True(bad.Count == 0, string.Join("; ", bad.Distinct()));
    }

    // Bookings chỉ được dùng đúng tập interface/DTO Customers đã cho phép (khóa cứng, không cho nới thêm).
    [Fact]
    public void BookingsModule_UsesOnlyApprovedCustomerContracts()
    {
        var allowed = new HashSet<string> { "ICustomerScreening", "ScreeningResult", "IDocumentVerification", "ILoyaltyService" };
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var used = typeof(BookingService).Assembly.GetTypes().Where(t => t.Namespace == "CarRental.Application.Modules.Bookings")
            .SelectMany(t => t.GetFields(flags).Select(f => f.FieldType)
                .Concat(t.GetConstructors(flags).SelectMany(c => c.GetParameters().Select(p => p.ParameterType))))
            .SelectMany(Flatten).Where(u => u.Namespace == "CarRental.Application.Modules.Customers").Select(u => u.Name).ToHashSet();
        Assert.True(used.IsSubsetOf(allowed), "Bookings dùng thêm: " + string.Join(", ", used.Except(allowed)));
        Assert.Contains("IDocumentVerification", used);
        Assert.Contains("ILoyaltyService", used);
        Assert.DoesNotContain("IDocumentService", used); // chỉ được dùng cổng hẹp IDocumentVerification
    }

    private static IEnumerable<Type> Flatten(Type t)
    {
        if (t.HasElementType) foreach (var e in Flatten(t.GetElementType()!)) yield return e;
        if (t.IsGenericType) foreach (var a in t.GetGenericArguments().SelectMany(Flatten)) yield return a;
        yield return t;
    }
}
