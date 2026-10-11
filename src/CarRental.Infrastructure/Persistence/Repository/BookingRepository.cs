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

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> HasOverlapAsync(int carId, DateTime startDate, DateTime endDate, int? excludeBookingId = null)
    {
        var query = _context.Bookings
            .Where(b => b.CarId == carId)
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

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public void Update(Booking booking)
    {
        _context.Bookings.Update(booking);
    }
}
