using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly ApplicationDbContext _context;
    public MaintenanceRepository(ApplicationDbContext context) => _context = context;

    public Task<CarMaintenance?> GetByIdAsync(int id)
        => _context.Set<CarMaintenance>().Include(m => m.Car).FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

    public async Task<int?> GetCarIdAsync(int id)
        => await _context.Set<CarMaintenance>().AsNoTracking().Where(m => m.Id == id && !m.IsDeleted).Select(m => (int?)m.CarId).FirstOrDefaultAsync();

    public Task<List<CarMaintenance>> ListAsync(int? carId, MaintenanceStatus? status)
    {
        var q = _context.Set<CarMaintenance>().Include(m => m.Car).Where(m => !m.IsDeleted);
        if (carId.HasValue) q = q.Where(m => m.CarId == carId);
        if (status.HasValue) q = q.Where(m => m.Status == status);
        return q.OrderBy(m => m.DueDate).Take(500).ToListAsync();
    }

    public Task<List<CarMaintenance>> ListPlannedAsync()
        => _context.Set<CarMaintenance>().Include(m => m.Car)
            .Where(m => !m.IsDeleted && m.Status == MaintenanceStatus.KeHoach).ToListAsync();

    public Task<bool> HasOtherInProgressAsync(int carId, int exceptId)
        => _context.Set<CarMaintenance>().AnyAsync(m => m.CarId == carId && m.Id != exceptId && m.Status == MaintenanceStatus.DangThucHien && !m.IsDeleted);

    // Số km chỉ tăng nên lấy lớn nhất của các biên bản bàn giao làm số km mới nhất.
    public async Task<Dictionary<int, int>> GetLatestOdometersAsync(IEnumerable<int> carIds)
    {
        var ids = carIds.ToList();
        return await _context.Set<HandoverRecord>()
            .Where(h => !h.IsDeleted && ids.Contains(h.Booking!.CarId))
            .GroupBy(h => h.Booking!.CarId)
            .Select(g => new { CarId = g.Key, Km = g.Max(h => h.Odometer) })
            .ToDictionaryAsync(x => x.CarId, x => x.Km);
    }

    public Task<List<int>> FindOverlappingBookingIdsAsync(int carId, DateTime from, DateTime to)
        => _context.Bookings
            .Where(b => b.CarId == carId && !b.IsDeleted
                && (b.Status == BookingStatus.ChoXacNhan || b.Status == BookingStatus.DaXacNhan)
                && b.StartDate < to && b.EndDate > from)
            .Select(b => b.Id).ToListAsync();

    public async Task AddAsync(CarMaintenance item) => await _context.Set<CarMaintenance>().AddAsync(item);
}
