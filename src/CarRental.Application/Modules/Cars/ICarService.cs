
using Microsoft.AspNetCore.Http;

namespace CarRental.Application.Modules.Cars;

public interface ICarService
{
    /// <summary>includePending: chỉ Staff/Admin mới thấy xe ChoDuyet.</summary>
    Task<(List<CarDto> Items, int TotalCount)> GetFilteredAsync(CarFilterRequest filter, bool includePending = false);
    Task<CarDto?> GetByIdAsync(int id, bool includePending = true);
    /// <summary>Admin tạo thì xe vào SanSang; Staff tạo thì xe vào ChoDuyet (giá chỉ là đề xuất).</summary>
    Task<CarDto> CreateAsync(CreateCarRequest request, bool isAdmin);
    Task UpdateAsync(int id, UpdateCarRequest request, bool isAdmin);
    Task<CarDto> ApproveAsync(int id, ApproveCarRequest? request);
    Task DeleteAsync(int id);
    Task<CarImageDto> AddImageAsync(int carId, IFormFile file);
    Task SetPrimaryImageAsync(int carId, int imageId);
    Task DeleteImageAsync(int carId, int imageId);
}

