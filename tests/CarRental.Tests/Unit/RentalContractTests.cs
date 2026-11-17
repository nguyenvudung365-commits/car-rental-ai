using System.Security.Claims;
using CarRental.API.Controllers;
using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using BookingStatus = CarRental.Domain.Common.BookingStatus;

namespace CarRental.Tests.Unit;

/// <summary>GetContractAsync (service) và GET /api/bookings/{id}/contract (controller) với repo giả.</summary>
public sealed class RentalContractTests
{
    private const int OwnerId = 7;

    private static Booking MakeBooking(BookingStatus status, ReturnRecord? rr = null) => new()
    {
        Id = 5, CustomerId = OwnerId, CarId = 3, Status = status,
        StartDate = new DateTime(2030, 1, 5), EndDate = new DateTime(2030, 1, 8),
        FinalPricePerDay = 700_000, TotalPrice = 2_100_000, CreatedAt = DateTime.UtcNow,
        Customer = new Customer { Id = OwnerId, FullName = "Lê Thị Bích", Email = "b@t.vn", PhoneNumber = "0900000001" },
        Car = new Car { Id = 3, Brand = "Mazda", Model = "CX-5", LicensePlate = "30A-111.11", Seats = 5 },
        ReturnRecord = rr
    };

    private static (RentalService svc, FakePdf pdf) NewService(Booking? b)
    {
        var pdf = new FakePdf();
        return (new RentalService(new FakeRepo(b), new FakeBookingService(), new FakeUow(), pdf), pdf);
    }

    [Theory]
    [InlineData(BookingStatus.ChoXacNhan)]
    [InlineData(BookingStatus.DaHuy)]
    public async Task GetContract_PendingOrCancelled_Throws409InvalidStatus(BookingStatus st)
    {
        var (svc, pdf) = NewService(MakeBooking(st));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => svc.GetContractAsync(5));
        Assert.Equal("INVALID_STATUS", ex.Code);
        Assert.Equal(0, pdf.Calls);
    }

    [Theory]
    [InlineData(BookingStatus.DaXacNhan)]
    [InlineData(BookingStatus.DangThue)]
    [InlineData(BookingStatus.DaTraXe)]
    public async Task GetContract_ConfirmedOnwards_ReturnsPdfFile(BookingStatus st)
    {
        var (svc, pdf) = NewService(MakeBooking(st));
        var file = await svc.GetContractAsync(5, OwnerId);
        Assert.Equal("hop-dong-5.pdf", file.FileName);
        Assert.NotEmpty(file.Content);
        Assert.Equal(1, pdf.Calls);
        Assert.Equal("Lê Thị Bích", pdf.Last!.CustomerName);
        Assert.Equal("Mazda CX-5", pdf.Last.CarName);
        Assert.Equal(3, pdf.Last.RentalDays);
        Assert.Equal(st.ToString(), pdf.Last.BookingStatus);
    }

    [Fact]
    public async Task GetContract_WrongOwner_Throws403_AndNoPdf()
    {
        var (svc, pdf) = NewService(MakeBooking(BookingStatus.DaXacNhan));
        await Assert.ThrowsAsync<ForbiddenException>(() => svc.GetContractAsync(5, OwnerId + 1));
        Assert.Equal(0, pdf.Calls);
    }

    [Fact]
    public async Task GetContract_StaffOrAdmin_NullCustomerId_CanDownloadAnyBooking()
    {
        var (svc, _) = NewService(MakeBooking(BookingStatus.DangThue));
        var file = await svc.GetContractAsync(5, null);
        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public async Task GetContract_NotFound_Throws()
    {
        var (svc, _) = NewService(null);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetContractAsync(999));
    }

    [Fact]
    public async Task GetContract_PendingBooking_WrongOwner_Gets403NotStatusLeak()
    {
        var (svc, _) = NewService(MakeBooking(BookingStatus.ChoXacNhan));
        await Assert.ThrowsAsync<ForbiddenException>(() => svc.GetContractAsync(5, OwnerId + 1));
    }

    [Fact]
    public async Task GetContract_WithoutReturnRecord_PassesNullReturnFields()
    {
        var (svc, pdf) = NewService(MakeBooking(BookingStatus.DangThue));
        await svc.GetContractAsync(5);
        Assert.Null(pdf.Last!.ReturnedAt);
        Assert.Null(pdf.Last.FinalAmount);
    }

    [Fact]
    public async Task GetContract_WithReturnRecord_PassesReturnFields()
    {
        var rr = new ReturnRecord { BookingId = 5, ReturnedAt = new DateTime(2030, 1, 10), LateDays = 2, LateFee = 1_400_000, DamageFee = 300_000, FinalAmount = 3_800_000 };
        var (svc, pdf) = NewService(MakeBooking(BookingStatus.DaTraXe, rr));
        await svc.GetContractAsync(5);
        Assert.Equal(2, pdf.Last!.LateDays);
        Assert.Equal(1_400_000m, pdf.Last.LateFee);
        Assert.Equal(300_000m, pdf.Last.DamageFee);
        Assert.Equal(3_800_000m, pdf.Last.FinalAmount);
    }

    // ---------- Controller ----------

    private static RentalController NewController(RentalService svc, params Claim[] claims)
    {
        var identity = claims.Length == 0 ? new ClaimsIdentity() : new ClaimsIdentity(claims, "test");
        return new RentalController(svc)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } }
        };
    }

    [Fact]
    public async Task Controller_Customer_OwnBooking_ReturnsApplicationPdfFile()
    {
        var (svc, _) = NewService(MakeBooking(BookingStatus.DaXacNhan));
        var ctl = NewController(svc, new Claim(ClaimTypes.NameIdentifier, OwnerId.ToString()), new Claim(ClaimTypes.Role, "Customer"));
        var result = await ctl.GetContract(5);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("hop-dong-5.pdf", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task Controller_Customer_OtherBooking_Throws403()
    {
        var (svc, _) = NewService(MakeBooking(BookingStatus.DaXacNhan));
        var ctl = NewController(svc, new Claim(ClaimTypes.NameIdentifier, "99"), new Claim(ClaimTypes.Role, "Customer"));
        await Assert.ThrowsAsync<ForbiddenException>(() => ctl.GetContract(5));
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("Admin")]
    public async Task Controller_StaffAndAdmin_CanDownloadAnyBooking(string role)
    {
        var (svc, _) = NewService(MakeBooking(BookingStatus.DangThue));
        var ctl = NewController(svc, new Claim(ClaimTypes.NameIdentifier, "99"), new Claim(ClaimTypes.Role, role));
        Assert.IsType<FileContentResult>(await ctl.GetContract(5));
    }

    [Fact]
    public async Task Controller_NoUserId_ReturnsUnauthorized()
    {
        var (svc, _) = NewService(MakeBooking(BookingStatus.DaXacNhan));
        var ctl = NewController(svc);
        Assert.IsType<UnauthorizedResult>(await ctl.GetContract(5));
    }

    [Theory]
    [InlineData(BookingStatus.ChoXacNhan)]
    [InlineData(BookingStatus.DaHuy)]
    public async Task Controller_PendingOrCancelled_Throws409(BookingStatus st)
    {
        var (svc, _) = NewService(MakeBooking(st));
        var ctl = NewController(svc, new Claim(ClaimTypes.NameIdentifier, OwnerId.ToString()), new Claim(ClaimTypes.Role, "Customer"));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => ctl.GetContract(5));
        Assert.Equal("INVALID_STATUS", ex.Code);
    }

    [Fact]
    public void Controller_ContractEndpoint_RequiresAuthentication_NotAnonymous()
    {
        var m = typeof(RentalController).GetMethod(nameof(RentalController.GetContract))!;
        Assert.NotEmpty(typeof(RentalController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(m.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        var http = (HttpGetAttribute)m.GetCustomAttributes(typeof(HttpGetAttribute), false).Single();
        Assert.Equal("api/bookings/{bookingId:int}/contract", http.Template);
    }

    // ---------- fakes ----------

    private sealed class FakePdf : IContractPdfGenerator
    {
        public int Calls { get; private set; }
        public ContractData? Last { get; private set; }
        public byte[] Generate(ContractData data) { Calls++; Last = data; return new byte[] { 0x25, 0x50, 0x44, 0x46 }; }
    }

    private sealed class FakeUow : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default) => action();
    }

    private sealed class FakeBookingService : IBookingService
    {
        public Task<BookingDto> CreateAsync(int customerId, CreateBookingRequest request) => throw new NotImplementedException();
        public Task<(List<BookingDto> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null) => throw new NotImplementedException();
        public Task<BookingDto> GetByIdAsync(int id, int? customerId = null) => throw new NotImplementedException();
        public Task<BookingDto> ConfirmAsync(int id) => throw new NotImplementedException();
        public Task<BookingDto> CancelAsync(int id, string? reason) => throw new NotImplementedException();
        public Task<BookingEstimateDto> EstimateAsync(EstimateBookingRequest request) => throw new NotImplementedException();
        public Task<List<BookedRangeDto>> GetBookedRangesAsync(int carId) => throw new NotImplementedException();
    }

    private sealed class FakeRepo(Booking? b) : IBookingRepository
    {
        public Task<Booking?> GetByIdAsync(int id, bool includeDetails = false) => Task.FromResult(b);
        public Task<(List<Booking> Items, int TotalCount)> GetFilteredAsync(BookingFilterRequest filter, int? customerId = null) => throw new NotImplementedException();
        public Task<bool> HasOverlapAsync(int carId, DateTime startDate, DateTime endDate, int? excludeBookingId = null) => throw new NotImplementedException();
        public Task<List<(DateTime Start, DateTime End)>> GetActiveRangesAsync(int carId, DateTime from) => throw new NotImplementedException();
        public Task AddAsync(Booking booking) => throw new NotImplementedException();
        public void Update(Booking booking) => throw new NotImplementedException();
        public Task<Payment?> GetPaymentByIdAsync(int id) => throw new NotImplementedException();
        public Task AddPaymentAsync(Payment payment) => throw new NotImplementedException();
        public Task AddReturnRecordAsync(ReturnRecord record) => throw new NotImplementedException();
    }
}
