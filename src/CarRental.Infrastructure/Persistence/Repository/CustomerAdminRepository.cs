using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class CustomerAdminRepository : ICustomerAdminRepository
{
    private readonly ApplicationDbContext _context;
    public CustomerAdminRepository(ApplicationDbContext context) => _context = context;

    // Đơn tính vào số liệu khách: chưa xóa và không bị hủy.
    private IQueryable<Booking> Bookings => _context.Bookings.Where(b => !b.IsDeleted && b.Status != BookingStatus.DaHuy);

    public async Task<(List<CustomerListRow> Items, int TotalCount)> SearchAsync(CustomerListFilterRequest f, int page, int pageSize)
    {
        var docs = _context.Set<CustomerDocument>().Where(d => !d.IsDeleted);
        var entries = _context.Set<BlacklistEntry>().Where(e => e.RemovedAt == null);
        var bookings = Bookings;

        var query = _context.Customers.AsNoTracking()
            .Where(c => c.Role == UserRole.Customer && !c.IsDeleted)
            .Select(c => new
            {
                Customer = c,
                Approved = docs.Count(d => d.CustomerId == c.Id && d.Status == DocumentStatus.DaDuyet),
                Rejected = docs.Count(d => d.CustomerId == c.Id && d.Status == DocumentStatus.TuChoi),
                Pending = docs.Count(d => d.CustomerId == c.Id && d.Status == DocumentStatus.ChoDuyet),
                // Khớp theo tài khoản, CCCD hoặc SĐT; ở danh sách SĐT so nguyên văn, trang chi tiết chuẩn hóa đầy đủ.
                Level = entries.Where(e => e.CustomerId == c.Id
                    || (c.IdentityNumber != null && e.IdentityNumber == c.IdentityNumber)
                    || (c.PhoneNumber != null && e.PhoneNumber == c.PhoneNumber))
                    .Max(e => (BlacklistLevel?)e.Level),
                BookingCount = bookings.Count(b => b.CustomerId == c.Id),
                Spent = bookings.Where(b => b.CustomerId == c.Id && b.Status == BookingStatus.DaTraXe)
                    .Sum(b => (decimal?)(b.ReturnRecord != null ? b.ReturnRecord.FinalAmount : b.TotalPrice)) ?? 0m
            })
            .Select(x => new
            {
                x.Customer, x.Level, x.BookingCount, x.Spent,
                Doc = x.Approved == 2 ? DocumentsOverallStatus.DaDuyet
                    : x.Rejected > 0 ? DocumentsOverallStatus.TuChoi
                    : x.Pending > 0 ? DocumentsOverallStatus.ChoDuyet
                    : DocumentsOverallStatus.ChuaNop
            });

        if (!string.IsNullOrEmpty(f.Search))
            query = query.Where(x => x.Customer.FullName.Contains(f.Search) || x.Customer.Email.Contains(f.Search)
                || x.Customer.PhoneNumber.Contains(f.Search));
        if (f.Tier is { } tier)
        {
            var (min, max) = LoyaltyPolicy.RangeOf(tier);
            query = query.Where(x => x.Customer.LifetimePoints >= min && (max == null || x.Customer.LifetimePoints < max));
        }
        if (f.DocStatus is { } doc) query = query.Where(x => x.Doc == doc);
        if (f.Blacklisted is { } bl) query = query.Where(x => (x.Level != null) == bl);

        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.Customer.CreatedAt).ThenBy(x => x.Customer.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (rows.Select(x => new CustomerListRow(x.Customer, x.Doc, x.Level, x.BookingCount, x.Spent)).ToList(), total);
    }

    public async Task<(int BookingCount, decimal TotalSpent)> GetStatsAsync(int customerId)
    {
        var count = await Bookings.CountAsync(b => b.CustomerId == customerId);
        var spent = await Bookings.Where(b => b.CustomerId == customerId && b.Status == BookingStatus.DaTraXe)
            .SumAsync(b => (decimal?)(b.ReturnRecord != null ? b.ReturnRecord.FinalAmount : b.TotalPrice)) ?? 0m;
        return (count, spent);
    }

    public Task<List<CustomerBookingBriefDto>> GetRecentBookingsAsync(int customerId, int take)
        => _context.Bookings.AsNoTracking().Where(b => !b.IsDeleted && b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt).Take(take)
            .Select(b => new CustomerBookingBriefDto(
                b.Id, b.Car != null ? b.Car.Brand + " " + b.Car.Model : "", b.StartDate, b.EndDate, b.TotalPrice, b.Status.ToString()))
            .ToListAsync();
}
