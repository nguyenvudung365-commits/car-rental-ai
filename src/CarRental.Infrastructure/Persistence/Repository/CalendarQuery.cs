using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

/// <summary>Lịch xe chỉ đọc: đơn thuê (trừ đơn hủy) và lịch bảo trì (trừ lịch hủy) giao với [from, to).</summary>
public class CalendarQuery : ICalendarQuery
{
    private readonly ApplicationDbContext _context;
    public CalendarQuery(ApplicationDbContext context) => _context = context;

    public async Task<List<CarCalendarDto>> GetAsync(DateTime from, DateTime to, int? branchId)
    {
        var cars = _context.Cars.AsNoTracking().Where(c => !c.IsDeleted && c.Status != CarStatus.ChoDuyet);
        if (branchId.HasValue) cars = cars.Where(c => c.BranchId == branchId);
        var carList = await cars.OrderBy(c => c.Id).Select(c => new { c.Id, c.Brand, c.Model, c.LicensePlate, c.Status }).ToListAsync();
        var ids = carList.Select(c => c.Id).ToList();

        var bookings = await _context.Bookings.AsNoTracking()
            .Where(b => !b.IsDeleted && b.Status != BookingStatus.DaHuy && ids.Contains(b.CarId) && b.StartDate < to && b.EndDate > from)
            .Select(b => new { b.Id, b.CarId, b.StartDate, b.EndDate, b.Status, Customer = b.Customer!.FullName })
            .ToListAsync();

        var maint = await _context.Set<CarMaintenance>().AsNoTracking()
            .Where(m => !m.IsDeleted && m.Status != MaintenanceStatus.Huy && ids.Contains(m.CarId))
            .Select(m => new { m.Id, m.CarId, m.Type, m.DueDate, m.StartedAt, m.CompletedAt, m.Status })
            .ToListAsync();

        return carList.Select(c =>
        {
            var items = bookings.Where(b => b.CarId == c.Id)
                .Select(b => new CalendarItemDto("Booking", b.Id, b.StartDate, b.EndDate, $"#{b.Id} {b.Customer}", b.Status.ToString()));
            var ms = maint.Where(m => m.CarId == c.Id)
                .Select(m =>
                {
                    var start = m.StartedAt ?? m.DueDate;
                    var end = m.CompletedAt ?? (m.DueDate.Date.AddDays(1) > start ? m.DueDate.Date.AddDays(1) : start.AddDays(1));
                    return new CalendarItemDto("Maintenance", m.Id, start, end, m.Type.ToString(), m.Status.ToString());
                })
                .Where(i => i.Start < to && i.End > from);
            return new CarCalendarDto(c.Id, $"{c.Brand} {c.Model}", c.LicensePlate, c.Status, items.Concat(ms).OrderBy(i => i.Start).ToList());
        }).ToList();
    }
}
