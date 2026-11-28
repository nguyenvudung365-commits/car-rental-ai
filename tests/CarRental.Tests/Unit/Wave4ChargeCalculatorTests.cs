using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Pricing;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Pricing;
using Moq;

namespace CarRental.Tests.Unit;

/// <summary>Đợt 4: khung tính phí (mã giảm giá, phụ phí, tài xế, giao nhận, một chiều) theo tiêu chí kế hoạch mục 18/30/31.</summary>
public class Wave4ChargeCalculatorTests
{
    private static readonly DateTime D0 = new(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IPricingRepository> _repo = new();
    private readonly PricingOptions _opt = new(); // tài xế 500k/ngày 80k/giờ, giao nhận 150k, một chiều 200k

    public Wave4ChargeCalculatorTests() => _repo.Setup(r => r.ListSurchargesAsync(It.IsAny<bool>())).ReturnsAsync(new List<Surcharge>());

    private BookingChargeCalculator Calc() => new(_repo.Object, _opt);

    private static Car NewCar(int? branch = null) => new() { Brand = "T", Model = "V", BasePricePerDay = 1_000_000m, BranchId = branch };

    private static Coupon NewCoupon(Action<Coupon>? tweak = null)
    {
        var c = new Coupon
        {
            Id = 7, Code = "SALE", Type = CouponType.PhanTram, Value = 10, ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1), IsActive = true
        };
        tweak?.Invoke(c);
        return c;
    }

    private void WithCoupon(Coupon c, int usedByCustomer = 0)
    {
        _repo.Setup(r => r.GetCouponByCodeAsync(c.Code)).ReturnsAsync(c);
        _repo.Setup(r => r.GetCouponAsync(c.Id)).ReturnsAsync(c);
        _repo.Setup(r => r.CountCouponUsageAsync(c.Id, It.IsAny<int>())).ReturnsAsync(usedByCustomer);
    }

    private Task<List<ChargeLine>> Run(decimal rental, BookingOptions o, RentalMode mode = RentalMode.TheoNgay, int days = 3, int? branch = null, int? customer = 1)
        => Calc().CalculateAsync(new ChargeContext(customer, NewCar(branch), D0, mode == RentalMode.TheoGio ? D0.AddHours(5) : D0.AddDays(days), mode, rental, o));

    // ---------- Mã giảm giá ----------

    [Fact]
    public async Task Percent_coupon_is_capped_by_MaxDiscount_and_is_negative_line()
    {
        WithCoupon(NewCoupon(c => c.MaxDiscount = 50_000));
        var lines = await Run(1_000_000, new BookingOptions(CouponCode: " sale "));
        var l = Assert.Single(lines);
        Assert.Equal(ChargeKind.GiamGia, l.Kind);
        Assert.Equal(-50_000m, l.Amount);
        Assert.Equal(7, l.CouponId);
    }

    [Fact]
    public async Task Fixed_coupon_larger_than_order_never_makes_total_negative()
    {
        WithCoupon(NewCoupon(c => { c.Type = CouponType.SoTien; c.Value = 5_000_000; }));
        var lines = await Run(1_000_000, new BookingOptions(CouponCode: "SALE"));
        Assert.Equal(0m, 1_000_000m + lines.Sum(x => x.Amount));
    }

    [Fact]
    public async Task Surcharges_are_added_before_discount_which_applies_on_subtotal()
    {
        _repo.Setup(r => r.ListSurchargesAsync(true)).ReturnsAsync(new List<Surcharge>
        {
            new() { Id = 1, Name = "Vệ sinh", Type = SurchargeType.CoDinh, Amount = 100_000, AutoApply = true }
        });
        WithCoupon(NewCoupon());
        var lines = await Run(1_000_000, new BookingOptions(CouponCode: "SALE"));
        Assert.Equal(2, lines.Count);
        Assert.Equal(ChargeKind.PhuPhi, lines[0].Kind);
        Assert.Equal(-110_000m, lines[1].Amount); // 10% của (1.000.000 + 100.000)
    }

    [Fact]
    public async Task Surcharge_types_fixed_perDay_percent()
    {
        _repo.Setup(r => r.ListSurchargesAsync(true)).ReturnsAsync(new List<Surcharge>
        {
            new() { Id = 1, Name = "A", Type = SurchargeType.CoDinh, Amount = 50_000 },
            new() { Id = 2, Name = "B", Type = SurchargeType.TheoNgay, Amount = 20_000 },
            new() { Id = 3, Name = "C", Type = SurchargeType.PhanTram, Amount = 5 },
        });
        var lines = await Run(2_000_000, new BookingOptions(), days: 3);
        Assert.Equal(new[] { 50_000m, 60_000m, 100_000m }, lines.Select(l => l.Amount).ToArray());
    }

    [Theory]
    [InlineData("expired", "COUPON_EXPIRED")]
    [InlineData("future", "COUPON_NOT_STARTED")]
    [InlineData("inactive", "COUPON_INVALID")]
    [InlineData("min", "COUPON_MIN_ORDER")]
    public async Task Invalid_coupon_is_400_with_code(string kind, string code)
    {
        WithCoupon(NewCoupon(c =>
        {
            if (kind == "expired") c.ValidTo = DateTime.UtcNow.AddMinutes(-1);
            if (kind == "future") c.ValidFrom = DateTime.UtcNow.AddMinutes(5);
            if (kind == "inactive") c.IsActive = false;
            if (kind == "min") c.MinOrderAmount = 2_000_000;
        }));
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Run(1_000_000, new BookingOptions(CouponCode: "SALE")));
        Assert.Equal(code, ex.Code);
    }

    [Fact]
    public async Task Unknown_coupon_is_400()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Run(1_000_000, new BookingOptions(CouponCode: "NOPE")));
        Assert.Equal("COUPON_INVALID", ex.Code);
    }

    [Fact]
    public async Task Coupon_at_total_limit_is_409_exhausted()
    {
        WithCoupon(NewCoupon(c => { c.UsageLimit = 5; c.UsedCount = 5; }));
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Run(1_000_000, new BookingOptions(CouponCode: "SALE")));
        Assert.Equal("COUPON_EXHAUSTED", ex.Code);
    }

    [Fact]
    public async Task Coupon_one_below_limit_is_ok_boundary()
    {
        WithCoupon(NewCoupon(c => { c.UsageLimit = 5; c.UsedCount = 4; }));
        Assert.Single(await Run(1_000_000, new BookingOptions(CouponCode: "SALE")));
    }

    [Fact]
    public async Task Customer_over_PerCustomerLimit_is_409_already_used()
    {
        WithCoupon(NewCoupon(), usedByCustomer: 1);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => Run(1_000_000, new BookingOptions(CouponCode: "SALE")));
        Assert.Equal("COUPON_ALREADY_USED", ex.Code);
    }

    [Fact]
    public async Task Estimate_without_customer_skips_per_customer_check()
    {
        WithCoupon(NewCoupon(), usedByCustomer: 99);
        Assert.Single(await Run(1_000_000, new BookingOptions(CouponCode: "SALE"), customer: null));
    }

    [Fact]
    public async Task Redeem_failure_of_conditional_update_is_409_exhausted()
    {
        _repo.Setup(r => r.TryRedeemCouponAsync(7)).ReturnsAsync(false);
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            Calc().RedeemAsync(1, new List<ChargeLine> { new(ChargeKind.GiamGia, "x", -1, CouponId: 7) }));
        Assert.Equal("COUPON_EXHAUSTED", ex.Code);
    }

    [Fact]
    public async Task Redeem_customer_already_at_limit_after_lock_is_409()
    {
        WithCoupon(NewCoupon(), usedByCustomer: 1);
        _repo.Setup(r => r.TryRedeemCouponAsync(7)).ReturnsAsync(true);
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            Calc().RedeemAsync(1, new List<ChargeLine> { new(ChargeKind.GiamGia, "x", -1, CouponId: 7) }));
        Assert.Equal("COUPON_ALREADY_USED", ex.Code);
    }

    [Fact]
    public async Task Release_gives_back_one_use_per_coupon_charge_and_skips_deleted()
    {
        await Calc().ReleaseAsync(new[]
        {
            new BookingCharge { CouponId = 7, Amount = -1 },
            new BookingCharge { CouponId = 8, Amount = -1, IsDeleted = true },
            new BookingCharge { Amount = 5 }
        });
        _repo.Verify(r => r.ReleaseCouponAsync(7), Times.Once);
        _repo.Verify(r => r.ReleaseCouponAsync(8), Times.Never);
    }

    // ---------- Tài xế, giao nhận, một chiều ----------

    [Fact]
    public async Task Driver_fee_per_day_and_per_hour()
    {
        var byDay = await Run(0, new BookingOptions(WithDriver: true), days: 3);
        Assert.Equal((ChargeKind.TaiXe, 1_500_000m), (byDay[0].Kind, byDay[0].Amount));
        var byHour = await Run(0, new BookingOptions(WithDriver: true), RentalMode.TheoGio);
        Assert.Equal(5 * 80_000m, byHour[0].Amount);
    }

    [Fact]
    public async Task Delivery_fee_is_150k_per_direction_and_blank_address_is_free()
    {
        Assert.Empty(await Run(0, new BookingOptions(DeliveryAddress: "  ", ReturnAddress: null)));
        var one = await Run(0, new BookingOptions(DeliveryAddress: "1 Lê Lợi"));
        Assert.Equal(150_000m, Assert.Single(one).Amount);
        var both = await Run(0, new BookingOptions(DeliveryAddress: "a", ReturnAddress: "b"));
        Assert.Equal(300_000m, both.Sum(l => l.Amount));
        Assert.All(both, l => Assert.Equal(ChargeKind.GiaoNhan, l.Kind));
    }

    [Fact]
    public async Task Address_over_300_chars_is_400_and_300_is_ok()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Run(0, new BookingOptions(DeliveryAddress: new string('x', 301))));
        Assert.Equal("ADDRESS_TOO_LONG", ex.Code);
        Assert.Single(await Run(0, new BookingOptions(DeliveryAddress: new string('x', 300))));
    }

    [Fact]
    public async Task OneWay_fee_only_when_return_branch_differs()
    {
        _repo.Setup(r => r.GetBranchAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Branch { Id = id, IsActive = true });
        Assert.Empty(await Run(0, new BookingOptions(ReturnBranchId: 1), branch: 1));
        var l = Assert.Single(await Run(0, new BookingOptions(ReturnBranchId: 2), branch: 1));
        Assert.Equal((ChargeKind.MotChieu, 200_000m), (l.Kind, l.Amount));
    }

    [Fact]
    public async Task Branch_rules_mismatch_inactive_missing()
    {
        _repo.Setup(r => r.GetBranchAsync(1)).ReturnsAsync(new Branch { Id = 1, IsActive = true });
        _repo.Setup(r => r.GetBranchAsync(2)).ReturnsAsync(new Branch { Id = 2, IsActive = false });
        var mismatch = await Assert.ThrowsAsync<BadRequestException>(() => Run(0, new BookingOptions(PickupBranchId: 1), branch: 5));
        Assert.Equal("BRANCH_MISMATCH", mismatch.Code);
        var inactive = await Assert.ThrowsAsync<BadRequestException>(() => Run(0, new BookingOptions(ReturnBranchId: 2), branch: 1));
        Assert.Equal("BRANCH_INVALID", inactive.Code);
        var missing = await Assert.ThrowsAsync<BadRequestException>(() => Run(0, new BookingOptions(ReturnBranchId: 99), branch: 1));
        Assert.Equal("BRANCH_INVALID", missing.Code);
    }

    [Fact]
    public void Pricing_option_defaults_match_plan_constants()
    {
        var o = new PricingOptions();
        Assert.Equal((500_000m, 80_000m, 150_000m), (o.DriverFeePerDay, o.DriverFeePerHour, o.DeliveryFee));
    }

    // ---------- NoCharges / BookingService 4 tham số ----------

    private static BookingService Svc(decimal? pricePerHour, decimal basePrice, decimal predicted)
    {
        var car = new Car { Id = 1, Brand = "T", Model = "V", BasePricePerDay = basePrice, Status = CarStatus.SanSang, PricePerHour = pricePerHour };
        var cars = new Mock<ICarRepository>();
        cars.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(car);
        var ai = new Mock<IAiPricingClient>();
        ai.Setup(a => a.PredictPriceAsync(It.IsAny<PredictPriceRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PredictPriceResult(predicted, false));
        return new BookingService(Mock.Of<IBookingRepository>(), cars.Object, ai.Object, Mock.Of<IUnitOfWork>());
    }

    [Fact]
    public async Task NoCharges_returns_no_lines_and_keeps_car_branch()
    {
        var n = NoCharges.Instance;
        Assert.Empty(await n.CalculateAsync(new ChargeContext(1, NewCar(), D0, D0.AddDays(1), RentalMode.TheoNgay, 1, new BookingOptions(CouponCode: "X", WithDriver: true))));
        Assert.Empty(await n.RedeemAsync(1, new List<ChargeLine> { new(ChargeKind.GiamGia, "x", -1, CouponId: 1) }));
        Assert.Equal((3, 3), await n.ResolveBranchesAsync(3, new BookingOptions()));
    }

    [Fact]
    public async Task BookingService_with_four_args_estimates_rental_only_total()
    {
        var e = await Svc(null, 700_000m, 700_000m).EstimateAsync(new EstimateBookingRequest(1, D0, D0.AddDays(2)));
        Assert.Equal(1_400_000m, e.TotalPrice);
        Assert.True(e.Charges is null || e.Charges.Count == 0);
    }

    // ---------- Thuê theo giờ (ước tính) ----------

    [Theory]
    [InlineData(4, 400_000)]
    [InlineData(5, 500_000)]
    [InlineData(12, 1_200_000)]
    public async Task Hourly_estimate_is_hours_times_PricePerHour(int hours, int expected)
    {
        var e = await Svc(100_000, 800_000m, 800_000m).EstimateAsync(new EstimateBookingRequest(1, D0, D0.AddHours(hours), Mode: RentalMode.TheoGio));
        Assert.Equal(expected, e.TotalPrice);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(13)]
    public async Task Hourly_outside_4_to_12_hours_is_400(int hours)
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc(100_000, 800_000m, 800_000m).EstimateAsync(new EstimateBookingRequest(1, D0, D0.AddHours(hours), Mode: RentalMode.TheoGio)));
        Assert.Equal("HOURLY_RANGE", ex.Code);
    }

    // Sửa theo ADR-010: không có PricePerHour thì không cho thuê theo giờ (trước đây khẳng định giá ngày / 8).
    [Fact]
    public async Task Hourly_without_PricePerHour_is_400_HOURLY_NOT_SUPPORTED()
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            Svc(null, 800_000m, 800_000m).EstimateAsync(new EstimateBookingRequest(1, D0, D0.AddHours(8), Mode: RentalMode.TheoGio)));
        Assert.Equal("HOURLY_NOT_SUPPORTED", ex.Code);
    }

    // ---------- Kiến trúc ----------

    [Fact]
    public void Layers_do_not_reference_outward()
    {
        var domain = typeof(Coupon).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
        Assert.DoesNotContain("CarRental.Application", domain);
        Assert.DoesNotContain("CarRental.Infrastructure", domain);
        var app = typeof(BookingChargeCalculator).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
        Assert.DoesNotContain("CarRental.Infrastructure", app);
        Assert.DoesNotContain("CarRental.API", app);
    }
}
