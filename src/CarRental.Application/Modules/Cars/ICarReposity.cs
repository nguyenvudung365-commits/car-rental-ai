using CarEntity = CarRental.Domain.Modules.Cars.Car;

namespace CarRental.Application.Modules.Cars;

public interface ICarRepository
{
    Task<CarEntity?> GetByIdAsync(int id, bool includeImages = false);
    Task<CarEntity?> GetByLicensePlateAsync(string licensePlate);
    Task<(List<CarEntity> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter);
    Task AddAsync(CarEntity car);
    void Update(CarEntity car);
    void Delete(CarEntity car);
}