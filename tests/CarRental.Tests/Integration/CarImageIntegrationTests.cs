using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests.Integration;

public sealed class CarImageIntegrationTests : IAsyncLifetime
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

    private async Task<int> SeedCarAsync()
    {
        await using var db = NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan,
            Brand = "Toyota",
            Model = "Vios",
            LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..6],
            Seats = 5,
            CarAgeYears = 2,
            BasePricePerDay = 700_000,
            Status = CarStatus.SanSang
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    [Fact]
    public async Task DeleteImageAsync_PrimaryRemoved_NextImagePromoted_FileDeleted()
    {
        if (!_available) return;
        var carId = await SeedCarAsync();
        await using (var db = NewContext())
        {
            db.CarImages.Add(new CarImage { CarId = carId, ImageUrl = "cars/a.webp", IsPrimary = true });
            db.CarImages.Add(new CarImage { CarId = carId, ImageUrl = "cars/b.webp", IsPrimary = false });
            await db.SaveChangesAsync();
        }

        var storage = new FakeStorage();
        await using var db2 = NewContext();
        var service = new CarService(new CarRepository(db2), storage, db2);
        var primary = await db2.CarImages.FirstAsync(i => i.CarId == carId && i.IsPrimary);

        await service.DeleteImageAsync(carId, primary.Id);

        await using var check = NewContext();
        var remaining = await check.CarImages.Where(i => i.CarId == carId).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("cars/b.webp", remaining[0].ImageUrl);
        Assert.True(remaining[0].IsPrimary);
        Assert.Equal(new[] { "cars/a.webp" }, storage.Deleted);
    }

    [Fact]
    public async Task DeleteImageAsync_ImageOfAnotherCar_ThrowsNotFound()
    {
        if (!_available) return;
        var carId = await SeedCarAsync();
        var otherCarId = await SeedCarAsync();
        int imageId;
        await using (var db = NewContext())
        {
            var image = new CarImage { CarId = otherCarId, ImageUrl = "cars/z.webp", IsPrimary = true };
            db.CarImages.Add(image);
            await db.SaveChangesAsync();
            imageId = image.Id;
        }

        await using var db2 = NewContext();
        var service = new CarService(new CarRepository(db2), new FakeStorage(), db2);
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteImageAsync(carId, imageId));
    }

    private sealed class FakeStorage : IFileStorageService
    {
        public List<string> Deleted { get; } = new();
        public Task<string> SaveAsync(IFormFile file, string folder) => Task.FromResult("cars/new.webp");
        public void Delete(string relativePath) => Deleted.Add(relativePath);
    }
}
