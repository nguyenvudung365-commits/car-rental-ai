using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests.Integration;

/// <summary>
/// Kiểm thử tích hợp trên SQL Server LocalDB (DB tạm, tự xóa). Tự bỏ qua nếu máy không có LocalDB.
/// Đặt CARRENTAL_TEST_CONN để dùng SQL Server khác.
/// </summary>
public sealed class BookingIntegrationTests : IAsyncLifetime
{
    private readonly string _dbName = "CarRentalTest_" + Guid.NewGuid().ToString("N");
    private string _conn = "";
    private bool _available;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("CARRENTAL_TEST_CONN")
            ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";
        _conn = server + ";Database=" + _dbName;
        try
        {
            await using var db = NewContext();
            await db.Database.MigrateAsync();
            _available = true;
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (!_available) return;
        await using var db = NewContext();
        await db.Database.EnsureDeletedAsync();
    }

    private ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_conn).Options);

    private static BookingService NewBookingService(ApplicationDbContext db) =>
        new(new BookingRepository(db), new CarRepository(db), new FakeAi(), db);

    private async Task<(int carId, int customerId)> SeedAsync(CarStatus status = CarStatus.SanSang)
    {
        await using var db = NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..6],
            Seats = 5, CarAgeYears = 2, BasePricePerDay = 700_000, Status = status
        };
        var customer = new Customer
        {
            FullName = "Test", Email = Guid.NewGuid().ToString("N") + "@t.vn", PhoneNumber = Guid.NewGuid().ToString("N")[..10],
            PasswordHash = "x"
        };
        db.Cars.Add(car);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return (car.Id, customer.Id);
    }

    private static CreateBookingRequest Req(int carId, int fromDays, int toDays) =>
        new(carId, DateTime.UtcNow.Date.AddDays(fromDays), DateTime.UtcNow.Date.AddDays(toDays), null);

    [Fact]
    public async Task CreateAsync_TwoConcurrentRequests_SameCar_OnlyOneSucceeds()
    {
        if (!_available) return;
        var (carId, customerId) = await SeedAsync();

        var tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = NewContext();
            try
            {
                await NewBookingService(db).CreateAsync(customerId, Req(carId, 5, 8));
                return true;
            }
            catch (ConflictException) { return false; }
            catch (Exception ex) when (ex is DbUpdateException || ex is Microsoft.Data.SqlClient.SqlException)
            {
                return false; // deadlock victim của Serializable cũng coi như bị từ chối
            }
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        await using var check = NewContext();
        Assert.Equal(1, await check.Bookings.CountAsync(b => b.CarId == carId));
        Assert.Equal(1, results.Count(r => r));
    }

    [Fact]
    public async Task CreateAsync_AdjacentRanges_BothSucceed()
    {
        if (!_available) return;
        var (carId, customerId) = await SeedAsync();
        await using var db = NewContext();
        var svc = NewBookingService(db);

        await svc.CreateAsync(customerId, Req(carId, 5, 8));
        await svc.CreateAsync(customerId, Req(carId, 8, 10)); // [Start, End) nên 8 không trùng

        Assert.Equal(2, await db.Bookings.CountAsync(b => b.CarId == carId));
    }

    [Fact]
    public async Task CreateAsync_SendsCarTypeAsNumber_AndSavesPredictedPrice()
    {
        if (!_available) return;
        var (carId, customerId) = await SeedAsync();
        await using var db = NewContext();
        var ai = new FakeAi();
        var svc = new BookingService(new BookingRepository(db), new CarRepository(db), ai, db);

        var dto = await svc.CreateAsync(customerId, Req(carId, 5, 7));

        Assert.Equal(1, ai.LastRequest!.CarType); // Sedan = 1
        Assert.Equal(900_000, dto.PredictedPricePerDay);
    }

    [Theory]
    [InlineData(BookingStatus.DangThue)]
    [InlineData(BookingStatus.DaHuy)]
    [InlineData(BookingStatus.DaTraXe)]
    public async Task CancelAsync_RejectsNonCancellableStatuses(BookingStatus status)
    {
        if (!_available) return;
        var (carId, customerId) = await SeedAsync();
        int bookingId;
        await using (var db = NewContext())
        {
            var b = new Booking
            {
                CustomerId = customerId, CarId = carId, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2),
                FinalPricePerDay = 700_000, TotalPrice = 1_400_000, Status = status, CorrelationId = Guid.NewGuid().ToString()
            };
            db.Bookings.Add(b);
            await db.SaveChangesAsync();
            bookingId = b.Id;
        }

        await using var db2 = NewContext();
        await Assert.ThrowsAsync<ConflictException>(() => NewBookingService(db2).CancelAsync(bookingId, "x"));
    }

    [Theory]
    [InlineData(BookingStatus.ChoXacNhan)]
    [InlineData(BookingStatus.DaXacNhan)]
    public async Task CancelAsync_AllowsPendingAndConfirmed(BookingStatus status)
    {
        if (!_available) return;
        var (carId, customerId) = await SeedAsync();
        await using var db = NewContext();
        var b = new Booking
        {
            CustomerId = customerId, CarId = carId, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2),
            FinalPricePerDay = 700_000, TotalPrice = 1_400_000, Status = status, CorrelationId = Guid.NewGuid().ToString()
        };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();

        var dto = await NewBookingService(db).CancelAsync(b.Id, "đổi lịch");

        Assert.Equal("DaHuy", dto.Status);
    }

    [Fact]
    public async Task EstimateAsync_ReturnsAiPriceAndTotal_WithoutCreatingBooking()
    {
        if (!_available) return;
        var (carId, _) = await SeedAsync();
        await using var db = NewContext();

        var est = await NewBookingService(db).EstimateAsync(
            new EstimateBookingRequest(carId, DateTime.UtcNow.Date.AddDays(5), DateTime.UtcNow.Date.AddDays(8)));

        Assert.Equal(3, est.RentalDays);
        Assert.Equal(900_000, est.FinalPricePerDay);
        Assert.Equal(2_700_000, est.TotalPrice);
        Assert.Equal(0, await db.Bookings.CountAsync(b => b.CarId == carId));
    }

    [Fact]
    public async Task GetBookedRangesAsync_ReturnsOnlyActiveFutureBookings()
    {
        if (!_available) return;
        var (carId, customerId) = await SeedAsync();
        await using var db = NewContext();
        Booking Make(int from, int to, BookingStatus s) => new()
        {
            CustomerId = customerId, CarId = carId, StartDate = DateTime.UtcNow.Date.AddDays(from), EndDate = DateTime.UtcNow.Date.AddDays(to),
            FinalPricePerDay = 700_000, TotalPrice = 1, Status = s, CorrelationId = Guid.NewGuid().ToString()
        };
        db.Bookings.AddRange(
            Make(3, 5, BookingStatus.DaXacNhan),
            Make(6, 8, BookingStatus.DaHuy),
            Make(-5, -2, BookingStatus.DaTraXe));
        await db.SaveChangesAsync();

        var ranges = await NewBookingService(db).GetBookedRangesAsync(carId);

        Assert.Single(ranges);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(3), ranges[0].StartDate);
    }

    private sealed class FakeAi : IAiPricingClient
    {
        public PredictPriceRequest? LastRequest { get; private set; }

        public Task<PredictPriceResult> PredictPriceAsync(PredictPriceRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new PredictPriceResult(900_000, IsFallback: false));
        }
    }

}
