using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Pricing;

namespace CarRental.Application.Modules.Pricing;

public interface IPricingRepository
{
    /// <summary>Không tracking, bỏ mã đã xóa.</summary>
    Task<Coupon?> GetCouponByCodeAsync(string code);
    Task<Coupon?> GetCouponAsync(int id);
    Task<List<Coupon>> ListCouponsAsync();
    Task AddCouponAsync(Coupon coupon);
    /// <summary>Số đơn còn hiệu lực (chưa hủy) của khách đã dùng mã.</summary>
    Task<int> CountCouponUsageAsync(int couponId, int customerId);
    /// <summary>UPDATE có điều kiện: false khi hết lượt hoặc mã ngừng; giữ khóa dòng mã đến hết transaction.</summary>
    Task<bool> TryRedeemCouponAsync(int couponId);
    Task ReleaseCouponAsync(int couponId);

    Task<List<Surcharge>> ListSurchargesAsync(bool autoApplyOnly);
    Task<Surcharge?> GetSurchargeAsync(int id);
    Task AddSurchargeAsync(Surcharge surcharge);

    Task<Branch?> GetBranchAsync(int id);
    Task<List<Branch>> ListBranchesAsync(bool includeInactive);
    Task AddBranchAsync(Branch branch);
    Task<bool> BranchHasCarsAsync(int branchId);

    Task<List<BookingCharge>> ListChargesAsync(int bookingId);
    Task AddChargeAsync(BookingCharge charge);
}
