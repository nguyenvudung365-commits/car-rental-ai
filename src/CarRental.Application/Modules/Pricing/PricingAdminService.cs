using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Pricing;

namespace CarRental.Application.Modules.Pricing;

/// <summary>Quản trị mã giảm giá, phụ phí, chi nhánh (Admin) và tra cứu mã cho khách.</summary>
public class PricingAdminService
{
    private readonly IPricingRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly BookingChargeCalculator _calculator;

    public PricingAdminService(IPricingRepository repo, IUnitOfWork uow, BookingChargeCalculator calculator)
    {
        _repo = repo;
        _uow = uow;
        _calculator = calculator;
    }

    // ---- Mã giảm giá ----
    public async Task<List<CouponDto>> ListCouponsAsync() => (await _repo.ListCouponsAsync()).Select(ToDto).ToList();

    public async Task<CouponDto> CreateCouponAsync(SaveCouponRequest r)
    {
        Validate(r);
        var code = BookingChargeCalculator.NormalizeCode(r.Code)!;
        if (await _repo.GetCouponByCodeAsync(code) != null)
            throw new ConflictException("COUPON_CODE_EXISTS", "Mã giảm giá đã tồn tại.");
        var c = new Coupon { Code = code };
        Apply(c, r);
        await _repo.AddCouponAsync(c);
        try
        {
            await _uow.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Đua tạo cùng mã: unique index chặn, Application không biết kiểu lỗi EF nên kiểm lại bằng truy vấn.
            if (await _repo.GetCouponByCodeAsync(code) != null)
                throw new ConflictException("COUPON_CODE_EXISTS", "Mã giảm giá đã tồn tại.");
            throw;
        }
        return ToDto(c);
    }

    public async Task<CouponDto> UpdateCouponAsync(int id, SaveCouponRequest r)
    {
        Validate(r);
        var c = await _repo.GetCouponAsync(id) ?? throw new NotFoundException("Coupon", id);
        if (BookingChargeCalculator.NormalizeCode(r.Code) != c.Code)
            throw new BadRequestException("COUPON_CODE_IMMUTABLE", "Không đổi mã sau khi tạo.");
        if (r.UsageLimit.HasValue && r.UsageLimit < c.UsedCount)
            throw new BadRequestException("COUPON_LIMIT", "Giới hạn lượt không được nhỏ hơn số lượt đã dùng.");
        Apply(c, r);
        c.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ToDto(c);
    }

    /// <summary>Khách kiểm tra mã trước khi đặt. orderAmount null thì bỏ qua đơn tối thiểu.</summary>
    public async Task<CouponCheckDto> CheckCouponAsync(int customerId, string code, decimal? orderAmount)
    {
        var c = await _calculator.CheckCouponAsync(code, customerId, orderAmount);
        return new CouponCheckDto(c.Code, c.Type, c.Value, c.MaxDiscount, c.MinOrderAmount, c.ValidTo);
    }

    private static void Validate(SaveCouponRequest r)
    {
        if (r.ValidFrom >= r.ValidTo) throw new BadRequestException("COUPON_RANGE", "Ngày bắt đầu phải trước ngày hết hạn.");
        if (r.Type == CouponType.PhanTram && r.Value > 100) throw new BadRequestException("COUPON_VALUE", "Phần trăm giảm tối đa 100.");
    }

    private static void Apply(Coupon c, SaveCouponRequest r)
    {
        c.Type = r.Type; c.Value = r.Value; c.MaxDiscount = r.MaxDiscount; c.MinOrderAmount = r.MinOrderAmount;
        c.ValidFrom = r.ValidFrom; c.ValidTo = r.ValidTo; c.UsageLimit = r.UsageLimit;
        c.PerCustomerLimit = r.PerCustomerLimit; c.IsActive = r.IsActive;
    }

    private static CouponDto ToDto(Coupon c) => new(c.Id, c.Code, c.Type, c.Value, c.MaxDiscount, c.MinOrderAmount,
        c.ValidFrom, c.ValidTo, c.UsageLimit, c.UsedCount, c.PerCustomerLimit, c.IsActive);

    // ---- Phụ phí ----
    public async Task<List<SurchargeDto>> ListSurchargesAsync() => (await _repo.ListSurchargesAsync(false)).Select(ToDto).ToList();

    public async Task<SurchargeDto> CreateSurchargeAsync(SaveSurchargeRequest r)
    {
        CheckSurcharge(r);
        var s = new Surcharge();
        Apply(s, r);
        await _repo.AddSurchargeAsync(s);
        await _uow.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<SurchargeDto> UpdateSurchargeAsync(int id, SaveSurchargeRequest r)
    {
        CheckSurcharge(r);
        var s = await _repo.GetSurchargeAsync(id) ?? throw new NotFoundException("Surcharge", id);
        Apply(s, r);
        s.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ToDto(s);
    }

    private static void CheckSurcharge(SaveSurchargeRequest r)
    {
        if (r.Type == SurchargeType.PhanTram && r.Amount > 100) throw new BadRequestException("SURCHARGE_VALUE", "Phần trăm tối đa 100.");
    }

    private static void Apply(Surcharge s, SaveSurchargeRequest r)
    {
        s.Name = r.Name.Trim(); s.Type = r.Type; s.Amount = r.Amount; s.AutoApply = r.AutoApply; s.IsActive = r.IsActive;
    }

    private static SurchargeDto ToDto(Surcharge s) => new(s.Id, s.Name, s.Type, s.Amount, s.AutoApply, s.IsActive);

    // ---- Chi nhánh ----
    public async Task<List<BranchDto>> ListBranchesAsync(bool includeInactive) =>
        (await _repo.ListBranchesAsync(includeInactive)).Select(ToDto).ToList();

    public async Task<BranchDto> CreateBranchAsync(SaveBranchRequest r)
    {
        var b = new Branch();
        Apply(b, r);
        await _repo.AddBranchAsync(b);
        await _uow.SaveChangesAsync();
        return ToDto(b);
    }

    public async Task<BranchDto> UpdateBranchAsync(int id, SaveBranchRequest r)
    {
        var b = await _repo.GetBranchAsync(id) ?? throw new NotFoundException("Branch", id);
        if (!r.IsActive && b.IsActive && await _repo.BranchHasCarsAsync(id))
            throw new ConflictException("BRANCH_HAS_CARS", "Chi nhánh còn xe, hãy chuyển xe sang chi nhánh khác trước khi ngừng hoạt động.");
        Apply(b, r);
        b.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return ToDto(b);
    }

    private static void Apply(Branch b, SaveBranchRequest r)
    {
        b.Name = r.Name.Trim(); b.Address = r.Address.Trim(); b.Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim(); b.IsActive = r.IsActive;
    }

    private static BranchDto ToDto(Branch b) => new(b.Id, b.Name, b.Address, b.Phone, b.IsActive);
}
