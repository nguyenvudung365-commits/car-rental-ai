using CarRental.Application.Modules.Reports;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class DashboardQuery : IDashboardQuery
{
    private const int ListLimit = 20;
    private readonly ApplicationDbContext _context;
    public DashboardQuery(ApplicationDbContext context) => _context = context;

    public async Task<DashboardDto> GetAsync(DateTime nowUtc, bool includeRevenue)
    {
        var statuses = await _context.Cars.AsNoTracking().Where(c => !c.IsDeleted)
            .GroupBy(c => c.Status).Select(g => new CarStatusCountDto(g.Key, g.Count())).ToListAsync();

        // Đơn đang thuê là tập nhỏ nên tính hạn trả ở bộ nhớ (xem ADR-011 về đơn theo ngày).
        var renting = (await _context.Bookings.AsNoTracking()
            .Where(b => !b.IsDeleted && b.Status == BookingStatus.DangThue)
            .Select(b => new
            {
                b.Id, b.EndDate, b.Mode, CarName = b.Car!.Brand + " " + b.Car.Model, b.Car.LicensePlate, Customer = b.Customer!.FullName
            }).ToListAsync())
            .Select(b => (Dto: new DashboardBookingDto(b.Id, b.CarName, b.LicensePlate, b.Customer, b.EndDate, b.Mode),
                          Due: b.Mode == RentalMode.TheoGio ? b.EndDate : b.EndDate.Date.AddDays(1)))
            .ToList();

        var overdue = renting.Where(r => r.Due < nowUtc).OrderBy(r => r.Due).Take(ListLimit).Select(r => r.Dto).ToList();
        var soon = renting.Where(r => r.Due >= nowUtc && r.Due < nowUtc.AddHours(24)).OrderBy(r => r.Due).Take(ListLimit).Select(r => r.Dto).ToList();

        var pendingConfirm = await _context.Bookings.CountAsync(b => !b.IsDeleted && b.Status == BookingStatus.ChoXacNhan);
        var pendingDocs = await _context.Set<CarRental.Domain.Modules.Customers.CustomerDocument>()
            .CountAsync(d => !d.IsDeleted && d.Status == DocumentStatus.ChoDuyet);
        var weekAhead = nowUtc.Date.AddDays(7);
        var maintenanceDue = await _context.Set<CarMaintenance>()
            .CountAsync(m => !m.IsDeleted && m.Status == MaintenanceStatus.KeHoach && m.DueDate <= weekAhead);

        decimal? revenue = null;
        if (includeRevenue)
        {
            var start = nowUtc.Date;
            // Cùng định nghĩa với báo cáo doanh thu: Payment đã thu theo PaidAt, không gồm cọc bảo đảm.
            revenue = await _context.Payments
                .Where(p => !p.IsDeleted && (p.Status == PaymentStatus.DaCoc || p.Status == PaymentStatus.DaThanhToan)
                         && p.PaidAt != null && p.PaidAt >= start && p.PaidAt < start.AddDays(1))
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
        }

        return new DashboardDto(statuses, renting.Count, soon, overdue, pendingConfirm, pendingDocs, maintenanceDue, revenue);
    }
}
