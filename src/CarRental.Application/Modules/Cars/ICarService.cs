
using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Modules.Cars;

public interface ICarService
{
    Task<(List<CarDto> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter);
    Task<CarDto?> GetByIdAsync(int id);
    Task<CarDto> CreateAsync(CreateCarRequest request);
    Task UpdateAsync(int id, UpdateCarRequest request);
    Task DeleteAsync(int id);
    Task<CarImageDto> AddImageAsync(int carId, IFormFile file);
    Task SetPrimaryImageAsync(int carId, int imageId);
}

