using CarRental.Application.Modules.Pricing;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Pricing;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repository;

public class PricingRepository : IPricingRepository
{
    private readonly ApplicationDbContext _db;
    public PricingRepository(ApplicationDbContext db) => _db = db;

    public Task<Coupon?> GetCouponByCodeAsync(string code) =>
        _db.Set<Coupon>().AsNoTracking().FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);

    public Task<Coupon?> GetCouponAsync(int id) => _db.Set<Coupon>().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public Task<List<Coupon>> ListCouponsAsync() =>
        _db.Set<Coupon>().AsNoTracking().Where(c => !c.IsDeleted).OrderByDescending(c => c.Id).ToListAsync();

    public async Task AddCouponAsync(Coupon coupon) => await _db.Set<Coupon>().AddAsync(coupon);

    public Task<int> CountCouponUsageAsync(int couponId, int customerId) =>
        _db.Set<BookingCharge>().CountAsync(c => c.CouponId == couponId && !c.IsDeleted
            && c.Booking!.CustomerId == customerId && !c.Booking.IsDeleted && c.Booking.Status != BookingStatus.DaHuy);

    // Điều kiện nằm trong UPDATE nên hai đơn cuối cùng không thể cùng vượt UsageLimit; dòng bị khóa tới hết transaction.
    public async Task<bool> TryRedeemCouponAsync(int couponId) =>
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Coupons SET UsedCount = UsedCount + 1
            WHERE Id = {couponId} AND IsActive = 1 AND IsDeleted = 0
              AND ValidFrom <= SYSUTCDATETIME() AND ValidTo >= SYSUTCDATETIME()
              AND (UsageLimit IS NULL OR UsedCount < UsageLimit)") == 1;

    public async Task ReleaseCouponAsync(int couponId) =>
        await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Coupons SET UsedCount = UsedCount - 1 WHERE Id = {couponId} AND UsedCount > 0");

    public Task<List<Surcharge>> ListSurchargesAsync(bool autoApplyOnly) =>
        _db.Set<Surcharge>().AsNoTracking()
            .Where(s => !s.IsDeleted && (!autoApplyOnly || (s.AutoApply && s.IsActive)))
            .OrderBy(s => s.Id).ToListAsync();

    public Task<Surcharge?> GetSurchargeAsync(int id) => _db.Set<Surcharge>().FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

    public async Task AddSurchargeAsync(Surcharge surcharge) => await _db.Set<Surcharge>().AddAsync(surcharge);

    public Task<Branch?> GetBranchAsync(int id) => _db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

    public Task<List<Branch>> ListBranchesAsync(bool includeInactive) =>
        _db.Set<Branch>().AsNoTracking().Where(b => !b.IsDeleted && (includeInactive || b.IsActive)).OrderBy(b => b.Name).ToListAsync();

    public async Task AddBranchAsync(Branch branch) => await _db.Set<Branch>().AddAsync(branch);

    public Task<bool> BranchHasCarsAsync(int branchId) => _db.Cars.AnyAsync(c => c.BranchId == branchId && !c.IsDeleted);

    public Task<List<BookingCharge>> ListChargesAsync(int bookingId) =>
        _db.Set<BookingCharge>().AsNoTracking().Where(c => c.BookingId == bookingId && !c.IsDeleted).OrderBy(c => c.Id).ToListAsync();

    public async Task AddChargeAsync(BookingCharge charge) => await _db.Set<BookingCharge>().AddAsync(charge);
}
