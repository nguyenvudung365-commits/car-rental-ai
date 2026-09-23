using CarRental.Application.Modules.Cars;
using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CarEntity = CarRental.Domain.Modules.Cars.Car;

namespace CarRental.Infrastructure.Persistence.Repository;

public class CarRepository : ICarRepository
{
    private readonly ApplicationDbContext _context;
    public CarRepository(ApplicationDbContext context) => _context = context;

    public async Task<CarEntity?> GetByIdAsync(int id, bool includeImages = false)
    {
        var query = _context.Cars.AsQueryable();
        if (includeImages) query = query.Include(c => c.Images);
        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<CarEntity?> GetByLicensePlateAsync(string licensePlate)
        => _context.Cars.FirstOrDefaultAsync(c => c.LicensePlate == licensePlate);

    public async Task<(List<CarEntity> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter)
    {
        var query = _context.Cars.Include(c => c.Images).AsQueryable();

        if (filter.CarType.HasValue)
            query = query.Where(c => c.CarType == filter.CarType);
        if (filter.MinPrice.HasValue)
            query = query.Where(c => c.BasePricePerDay >= filter.MinPrice);
        if (filter.MaxPrice.HasValue)
            query = query.Where(c => c.BasePricePerDay <= filter.MaxPrice);
        if (filter.Status.HasValue)
            query = query.Where(c => c.Status == filter.Status);

        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(CarEntity car) => await _context.Cars.AddAsync(car);
    public void Update(CarEntity car) => _context.Cars.Update(car);
    public void Delete(CarEntity car) => _context.Cars.Remove(car);
}