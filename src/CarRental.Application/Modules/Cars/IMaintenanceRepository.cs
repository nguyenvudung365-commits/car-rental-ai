using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;

namespace CarRental.Application.Modules.Cars;

public interface IMaintenanceRepository
{
    Task<CarMaintenance?> GetByIdAsync(int id);
    /// <summary>Chỉ lấy CarId, không tracking (để khóa xe trước khi nạp lịch có tracking).</summary>
    async Task<int?> GetCarIdAsync(int id) => (await GetByIdAsync(id))?.CarId;
    Task<List<CarMaintenance>> ListAsync(int? carId, MaintenanceStatus? status);
    /// <summary>Các lịch còn ở trạng thái kế hoạch, kèm xe, để tính nhắc hạn.</summary>
    Task<List<CarMaintenance>> ListPlannedAsync();
    Task<bool> HasOtherInProgressAsync(int carId, int exceptId);
    /// <summary>Số km ở biên bản bàn giao mới nhất của mỗi xe.</summary>
    Task<Dictionary<int, int>> GetLatestOdometersAsync(IEnumerable<int> carIds);
    /// <summary>Đơn chờ xác nhận/đã xác nhận chồng khoảng thời gian.</summary>
    Task<List<int>> FindOverlappingBookingIdsAsync(int carId, DateTime from, DateTime to);
    Task AddAsync(CarMaintenance item);
}
