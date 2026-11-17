using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests.Integration;

/// <summary>Booking 1-N Payment trên SQL Server LocalDB (tự bỏ qua nếu không có LocalDB).</summary>
public sealed class PaymentBookingRelationTests : IAsyncLifetime
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

    [Fact]
    public async Task Booking_CanHaveMultiplePayments()
    {
        if (!_available) return;

        int bookingId;
        await using (var db = NewContext())
        {
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

            var booking = new Booking
            {
                CustomerId = customer.Id, CarId = car.Id,
                StartDate = DateTime.UtcNow.Date.AddDays(1), EndDate = DateTime.UtcNow.Date.AddDays(3),
                FinalPricePerDay = 700_000, TotalPrice = 1_400_000
            };
            booking.Payments.Add(new Payment { Amount = 500_000, Method = default, Status = PaymentStatus.ChuaThanhToan });
            booking.Payments.Add(new Payment { Amount = 900_000, Method = default, Status = PaymentStatus.ChuaThanhToan });
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            bookingId = booking.Id;
        }

        await using var verify = NewContext();
        var payments = await verify.Payments.Where(p => p.BookingId == bookingId).ToListAsync();
        Assert.Equal(2, payments.Count);
        Assert.Equal(1_400_000, payments.Sum(p => p.Amount));
    }
}
