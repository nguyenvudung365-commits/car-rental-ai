using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Pricing;

namespace CarRental.Application.Modules.Pricing;

public class BookingChargeCalculator : IBookingChargeCalculator
{
    public const int MaxAddressLength = 300;

    private readonly IPricingRepository _repo;
    private readonly PricingOptions _options;

    public BookingChargeCalculator(IPricingRepository repo, PricingOptions options)
    {
        _repo = repo;
        _options = options;
    }

    public async Task<List<ChargeLine>> CalculateAsync(ChargeContext ctx)
    {
        var o = ctx.Options;
        var hourly = ctx.Mode == RentalMode.TheoGio;
        var hours = (int)Math.Ceiling((ctx.End - ctx.Start).TotalHours);
        var days = hourly ? 1 : Math.Max(1, (ctx.End - ctx.Start).Days);
        var lines = new List<ChargeLine>();

        foreach (var s in await _repo.ListSurchargesAsync(autoApplyOnly: true))
        {
            var amount = s.Type switch
            {
                SurchargeType.CoDinh => s.Amount,
                SurchargeType.TheoNgay => s.Amount * days,
                _ => ctx.RentalAmount * s.Amount / 100m
            };
            amount = Math.Round(amount, 0);
            if (amount > 0) lines.Add(new ChargeLine(ChargeKind.PhuPhi, s.Name, amount, SurchargeId: s.Id));
        }

        if (o.WithDriver)
            lines.Add(new ChargeLine(ChargeKind.TaiXe, "Tài xế riêng",
                hourly ? hours * _options.DriverFeePerHour : days * _options.DriverFeePerDay));

        var delivery = Clean(o.DeliveryAddress);
        var back = Clean(o.ReturnAddress);
        if (delivery != null) lines.Add(new ChargeLine(ChargeKind.GiaoNhan, "Giao xe tận nơi", _options.DeliveryFee));
        if (back != null) lines.Add(new ChargeLine(ChargeKind.GiaoNhan, "Nhận xe tận nơi", _options.DeliveryFee));

        var (pickup, ret) = await ResolveBranchesAsync(ctx.Car.BranchId, o);
        if (pickup.HasValue && ret.HasValue && pickup != ret)
            lines.Add(new ChargeLine(ChargeKind.MotChieu, "Phí trả xe khác chi nhánh", _options.OneWayFee));

        var code = NormalizeCode(o.CouponCode);
        if (code != null)
        {
            var baseAmount = ctx.RentalAmount + lines.Sum(l => l.Amount);
            var coupon = await CheckCouponAsync(code, ctx.CustomerId, baseAmount);
            var discount = coupon.Type == CouponType.PhanTram ? baseAmount * coupon.Value / 100m : coupon.Value;
            if (coupon.MaxDiscount.HasValue) discount = Math.Min(discount, coupon.MaxDiscount.Value);
            discount = Math.Round(Math.Min(discount, baseAmount), 0);
            if (discount > 0) lines.Add(new ChargeLine(ChargeKind.GiamGia, $"Mã giảm giá {coupon.Code}", -discount, CouponId: coupon.Id));
        }
        return lines;
    }

    public async Task<List<BookingCharge>> RedeemAsync(int customerId, List<ChargeLine> lines)
    {
        foreach (var line in lines.Where(l => l.CouponId.HasValue))
        {
            if (!await _repo.TryRedeemCouponAsync(line.CouponId!.Value))
                throw new ConflictException("COUPON_EXHAUSTED", "Mã giảm giá đã hết lượt sử dụng.");
            // Sau khi giữ khóa dòng mã, đếm lại lượt của khách: hai đơn đồng thời của cùng khách không cùng vượt giới hạn.
            var coupon = await _repo.GetCouponAsync(line.CouponId.Value);
            if (coupon != null && await _repo.CountCouponUsageAsync(coupon.Id, customerId) >= coupon.PerCustomerLimit)
                throw new ConflictException("COUPON_ALREADY_USED", "Bạn đã dùng hết số lần cho mã giảm giá này.");
        }
        return lines.Select(l => new BookingCharge
        {
            Kind = l.Kind, Name = l.Name, Amount = l.Amount, CouponId = l.CouponId, SurchargeId = l.SurchargeId
        }).ToList();
    }

    public async Task ReleaseAsync(IEnumerable<BookingCharge> charges)
    {
        foreach (var c in charges.Where(c => c.CouponId.HasValue && !c.IsDeleted))
            await _repo.ReleaseCouponAsync(c.CouponId!.Value);
    }

    /// <summary>Kiểm tra mã: tồn tại, đang bật, trong hạn, đủ giá trị tối thiểu, còn lượt, khách chưa vượt giới hạn.</summary>
    public async Task<Coupon> CheckCouponAsync(string code, int? customerId, decimal? orderAmount)
    {
        var coupon = await _repo.GetCouponByCodeAsync(NormalizeCode(code) ?? "")
            ?? throw new BadRequestException("COUPON_INVALID", "Mã giảm giá không tồn tại.");
        var now = DateTime.UtcNow;
        if (!coupon.IsActive) throw new BadRequestException("COUPON_INVALID", "Mã giảm giá đã ngừng áp dụng.");
        if (now < coupon.ValidFrom) throw new BadRequestException("COUPON_NOT_STARTED", "Mã giảm giá chưa đến thời gian áp dụng.");
        if (now > coupon.ValidTo) throw new BadRequestException("COUPON_EXPIRED", "Mã giảm giá đã hết hạn.");
        if (orderAmount.HasValue && coupon.MinOrderAmount.HasValue && orderAmount < coupon.MinOrderAmount)
            throw new BadRequestException("COUPON_MIN_ORDER", $"Đơn tối thiểu {coupon.MinOrderAmount:N0} đ để dùng mã này.");
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit)
            throw new ConflictException("COUPON_EXHAUSTED", "Mã giảm giá đã hết lượt sử dụng.");
        if (customerId.HasValue && await _repo.CountCouponUsageAsync(coupon.Id, customerId.Value) >= coupon.PerCustomerLimit)
            throw new ConflictException("COUPON_ALREADY_USED", "Bạn đã dùng hết số lần cho mã giảm giá này.");
        return coupon;
    }

    public static string? NormalizeCode(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

    private static string? Clean(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var a = address.Trim();
        if (a.Length > MaxAddressLength)
            throw new BadRequestException("ADDRESS_TOO_LONG", $"Địa chỉ tối đa {MaxAddressLength} ký tự.");
        return a;
    }

    /// <summary>Chi nhánh nhận = lựa chọn hoặc chi nhánh hiện tại của xe; chi nhánh trả mặc định là chi nhánh nhận.</summary>
    public async Task<(int? Pickup, int? Return)> ResolveBranchesAsync(int? carBranchId, BookingOptions o)
    {
        var pickup = o.PickupBranchId ?? carBranchId;
        var ret = o.ReturnBranchId ?? pickup;
        if (o.PickupBranchId.HasValue && carBranchId.HasValue && o.PickupBranchId != carBranchId)
            throw new BadRequestException("BRANCH_MISMATCH", "Xe không đỗ ở chi nhánh nhận đã chọn.");
        foreach (var id in new[] { o.PickupBranchId, o.ReturnBranchId }.Where(i => i.HasValue).Select(i => i!.Value).Distinct())
        {
            var b = await _repo.GetBranchAsync(id);
            if (b is null || !b.IsActive) throw new BadRequestException("BRANCH_INVALID", "Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
        }
        return (pickup, ret);
    }
}
