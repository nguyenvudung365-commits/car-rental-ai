using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int id, bool includeDetails = false)
    {
        var query = _context.Bookings.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(b => b.Customer)
                .Include(b => b.Car)
                .Include(b => b.Payments)
                .Include(b => b.ReturnRecord);
        }

        return await query.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<(List<Booking> Items, int TotalCount)> GetFilteredAsync(
        BookingFilterRequest filter,
        int? customerId = null)
    {
        var query = _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Car)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == customerId.Value);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == filter.CustomerId.Value);
        }

        if (filter.CarId.HasValue)
        {
            query = query.Where(b => b.CarId == filter.CarId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(b => b.Status.ToString() == filter.Status);
        }

        if (filter.StartDateFrom.HasValue)
        {
            query = query.Where(b => b.StartDate >= filter.StartDateFrom.Value);
        }

        if (filter.StartDateTo.HasValue)
        {
            query = query.Where(b => b.StartDate <= filter.StartDateTo.Value);
        }

        var totalCount = await query.CountAsync();

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> HasOverlapAsync(int carId, DateTime startDate, DateTime endDate, int? excludeBookingId = null)
    {
        var query = _context.Bookings
            .Where(b => b.CarId == carId && !b.IsDeleted)
            .Where(b => b.Status == BookingStatus.ChoXacNhan
                     || b.Status == BookingStatus.DaXacNhan
                     || b.Status == BookingStatus.DangThue)
            .Where(b => b.StartDate < endDate && b.EndDate > startDate);

        if (excludeBookingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookingId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<(DateTime Start, DateTime End)>> GetActiveRangesAsync(int carId, DateTime from)
    {
        var rows = await _context.Bookings
            .Where(b => b.CarId == carId && !b.IsDeleted && b.EndDate > from)
            .Where(b => b.Status == BookingStatus.ChoXacNhan
                     || b.Status == BookingStatus.DaXacNhan
                     || b.Status == BookingStatus.DangThue)
            .OrderBy(b => b.StartDate)
            .Select(b => new { b.StartDate, b.EndDate })
            .ToListAsync();
        return rows.Select(r => (r.StartDate, r.EndDate)).ToList();
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public void Update(Booking booking)
    {
        _context.Bookings.Update(booking);
    }

    public async Task<Payment?> GetPaymentByIdAsync(int id)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public async Task AddReturnRecordAsync(ReturnRecord record)
    {
        await _context.ReturnRecords.AddAsync(record);
    }
}
