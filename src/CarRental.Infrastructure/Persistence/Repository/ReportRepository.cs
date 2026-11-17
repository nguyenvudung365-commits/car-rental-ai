using CarRental.Application.Modules.Reports;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;
    public ReportRepository(ApplicationDbContext context) => _context = context;

    public async Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to)
    {
        var end = to.AddDays(1);
        var rows = await _context.Payments
            .Where(p => !p.IsDeleted
                     && (p.Status == PaymentStatus.DaCoc || p.Status == PaymentStatus.DaThanhToan)
                     && p.PaidAt != null && p.PaidAt >= from && p.PaidAt < end)
            .Select(p => new { p.BookingId, p.Amount, PaidAt = p.PaidAt!.Value })
            .ToListAsync();

        var byMonth = rows
            .GroupBy(r => new { r.PaidAt.Year, r.PaidAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new RevenueByMonthDto(
                $"{g.Key.Year:0000}-{g.Key.Month:00}",
                g.Sum(r => r.Amount),
                g.Select(r => r.BookingId).Distinct().Count()))
            .ToList();

        return new RevenueReportDto(
            rows.Sum(r => r.Amount),
            rows.Select(r => r.BookingId).Distinct().Count(),
            byMonth);
    }

    public async Task<OccupancyReportDto> GetOccupancyAsync(DateTime from, DateTime to)
    {
        var end = to.AddDays(1);
        var totalDays = (end - from).Days;

        var cars = await _context.Cars
            .Where(c => !c.IsDeleted && c.Status != CarStatus.NgungHoatDong)
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.LicensePlate })
            .ToListAsync();

        var bookings = await _context.Bookings
            .Where(b => !b.IsDeleted
                     && (b.Status == BookingStatus.DaXacNhan || b.Status == BookingStatus.DangThue || b.Status == BookingStatus.DaTraXe)
                     && b.StartDate < end && b.EndDate > from)
            .Select(b => new { b.CarId, b.StartDate, b.EndDate })
            .ToListAsync();

        // Đơn của một xe không chồng lịch (đã chặn khi đặt), nên cộng số ngày giao với khoảng báo cáo.
        var rentedDays = bookings
            .GroupBy(b => b.CarId)
            .ToDictionary(g => g.Key, g => g.Sum(b =>
            {
                var s = b.StartDate.Date > from ? b.StartDate.Date : from;
                var e = b.EndDate.Date < end ? b.EndDate.Date : end;
                return Math.Max(0, (e - s).Days);
            }));

        var byCar = cars
            .Select(c => new CarOccupancyDto(
                c.Id, c.LicensePlate,
                Math.Min(1.0, rentedDays.GetValueOrDefault(c.Id) / (double)totalDays)))
            .ToList();

        return new OccupancyReportDto(
            byCar.Count,
            byCar.Count == 0 ? 0 : byCar.Average(c => c.OccupancyRate),
            byCar);
    }
}
