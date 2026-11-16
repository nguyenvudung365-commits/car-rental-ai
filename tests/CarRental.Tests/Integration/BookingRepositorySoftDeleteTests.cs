using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests.Integration;

/// <summary>HasOverlapAsync / GetActiveRangesAsync phai bo qua booking IsDeleted = true (LocalDB, tu bo qua neu khong co).</summary>
public sealed class BookingRepositorySoftDeleteTests : IAsyncLifetime
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

    private async Task<int> SeedBookingAsync(int fromDays, int toDays, bool isDeleted, BookingStatus status = BookingStatus.DaXacNhan)
    {
        await using var db = NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..6],
            Seats = 5, CarAgeYears = 2, BasePricePerDay = 700_000, Status = CarStatus.SanSang
        };
        var customer = new Customer
        {
            FullName = "Test", Email = Guid.NewGuid().ToString("N") + "@t.vn", PhoneNumber = Guid.NewGuid().ToString("N")[..10],
            PasswordHash = "x"
        };
        db.Cars.Add(car);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        db.Bookings.Add(new Booking
        {
            CustomerId = customer.Id, CarId = car.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(fromDays), EndDate = DateTime.UtcNow.Date.AddDays(toDays),
            FinalPricePerDay = 700_000, TotalPrice = 1_400_000, Status = status,
            IsDeleted = isDeleted, CorrelationId = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();
        return car.Id;
    }

    [Fact]
    public async Task HasOverlap_IgnoresSoftDeletedBooking()
    {
        if (!_available) return;
        var carId = await SeedBookingAsync(3, 6, isDeleted: true);
        await using var db = NewContext();

        var overlap = await new BookingRepository(db).HasOverlapAsync(
            carId, DateTime.UtcNow.Date.AddDays(4), DateTime.UtcNow.Date.AddDays(5));

        Assert.False(overlap);
    }

    [Fact]
    public async Task HasOverlap_DetectsNonDeletedBooking()
    {
        if (!_available) return;
        var carId = await SeedBookingAsync(3, 6, isDeleted: false);
        await using var db = NewContext();

        var overlap = await new BookingRepository(db).HasOverlapAsync(
            carId, DateTime.UtcNow.Date.AddDays(4), DateTime.UtcNow.Date.AddDays(5));

        Assert.True(overlap);
    }

    [Fact]
    public async Task GetActiveRanges_IgnoresSoftDeletedBooking()
    {
        if (!_available) return;
        var carId = await SeedBookingAsync(3, 6, isDeleted: true);
        await using var db = NewContext();

        var ranges = await new BookingRepository(db).GetActiveRangesAsync(carId, DateTime.UtcNow.Date);

        Assert.Empty(ranges);
    }

    [Fact]
    public async Task GetActiveRanges_ReturnsNonDeletedBooking()
    {
        if (!_available) return;
        var carId = await SeedBookingAsync(3, 6, isDeleted: false);
        await using var db = NewContext();

        var ranges = await new BookingRepository(db).GetActiveRangesAsync(carId, DateTime.UtcNow.Date);

        Assert.Single(ranges);
    }
}
