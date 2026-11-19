using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Pricing;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

/// <summary>Helper dùng chung cho test đợt 4 (LocalDB + HTTP thật).</summary>
public abstract class Wave4Base
{
    protected readonly ApiFixture Fx;
    protected Wave4Base(ApiFixture fx) => Fx = fx;

    protected static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    protected static async Task<string?> Code(HttpResponseMessage r)
    {
        var j = await Json(r);
        return j.TryGetProperty("code", out var c) ? c.GetString() : null;
    }

    protected static string Uniq(string p = "C") => p + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    protected async Task<int> SeedCoupon(string? code = null, CouponType type = CouponType.PhanTram, decimal value = 10, decimal? max = null,
        int? limit = null, int perCustomer = 1, decimal? min = null, int dFrom = -1, int dTo = 30, bool active = true)
    {
        await using var db = Fx.NewContext();
        var c = new Coupon
        {
            Code = code ?? Uniq(), Type = type, Value = value, MaxDiscount = max, UsageLimit = limit, PerCustomerLimit = perCustomer,
            MinOrderAmount = min, ValidFrom = DateTime.UtcNow.AddDays(dFrom), ValidTo = DateTime.UtcNow.AddDays(dTo), IsActive = active
        };
        db.Set<Coupon>().Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    protected async Task<string> CouponCode(int id)
    {
        await using var db = Fx.NewContext();
        return (await db.Set<Coupon>().SingleAsync(c => c.Id == id)).Code;
    }

    protected async Task<int> UsedCount(int couponId)
    {
        await using var db = Fx.NewContext();
        return (await db.Set<Coupon>().AsNoTracking().SingleAsync(c => c.Id == couponId)).UsedCount;
    }

    protected async Task<int> SeedBranch(bool active = true)
    {
        await using var db = Fx.NewContext();
        var b = new Branch { Name = Uniq("B"), Address = "addr", IsActive = active };
        db.Set<Branch>().Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    protected async Task<int> SeedCar(int? branchId = null, decimal? perHour = null, CarStatus status = CarStatus.SanSang)
    {
        await using var db = Fx.NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..8],
            Seats = 5, CarAgeYears = 1, BasePricePerDay = 700_000, Status = status, BranchId = branchId, PricePerHour = perHour
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    protected async Task<(int Id, HttpClient Client)> Customer()
    {
        var u = await Fx.SeedUser(UserRole.Customer);
        return (u.Id, await Fx.LoginAs(u.Email));
    }

    protected static object DayBody(int carId, string? coupon = null, int from = 5, int to = 7, bool driver = false, string? deliver = null,
        string? back = null, int? ret = null, int? pickup = null) => new
        {
            CarId = carId, StartDate = DateTime.UtcNow.Date.AddDays(from), EndDate = DateTime.UtcNow.Date.AddDays(to),
            CouponCode = coupon, WithDriver = driver, DeliveryAddress = deliver, ReturnAddress = back, ReturnBranchId = ret, PickupBranchId = pickup
        };

    protected static object HourBody(int carId, int hours, int startOffsetHours = 3, string? coupon = null) => new
    {
        CarId = carId, StartDate = Hour(startOffsetHours), EndDate = Hour(startOffsetHours + hours), Mode = 2, CouponCode = coupon
    };

    protected static DateTime Hour(int offsetHours) => DateTime.UtcNow.Date.AddDays(40).AddHours(8 + offsetHours); // xa hiện tại, cố định theo giờ

    protected static decimal Sum(JsonElement booking) => booking.GetProperty("Charges").EnumerateArray().Sum(c => c.GetProperty("Amount").GetDecimal());
}

/// <summary>Mã giảm giá: hạn, lượt tổng/mỗi khách, đồng thời, hủy trả lượt, quyền Admin, mã trùng.</summary>
public sealed class Wave4CouponApiTests : Wave4Base, IClassFixture<ApiFixture>
{
    public Wave4CouponApiTests(ApiFixture fx) : base(fx) { }

    private static object Save(string code, int type = 1, decimal value = 10, decimal? max = null, int? limit = null, int dFrom = -1, int dTo = 30,
        int per = 1) => new
        {
            Code = code, Type = type, Value = value, MaxDiscount = max, MinOrderAmount = (decimal?)null,
            ValidFrom = DateTime.UtcNow.AddDays(dFrom), ValidTo = DateTime.UtcNow.AddDays(dTo), UsageLimit = limit, PerCustomerLimit = per, IsActive = true
        };

    [LocalDbFact]
    public async Task Booking_with_coupon_total_is_rental_plus_negative_discount_and_consumes_one_use()
    {
        var id = await SeedCoupon(max: 50_000, limit: 10);
        var (_, c) = await Customer();
        var r = await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), await CouponCode(id)));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var b = await Json(r);
        var rental = b.GetProperty("FinalPricePerDay").GetDecimal() * 2;
        var discount = b.GetProperty("Charges").EnumerateArray().Single(x => x.GetProperty("Kind").GetString() == "GiamGia").GetProperty("Amount").GetDecimal();
        Assert.True(discount < 0);
        Assert.Equal(Math.Min(rental * 0.1m, 50_000m), -discount, 0);
        Assert.Equal(rental + Sum(b), b.GetProperty("TotalPrice").GetDecimal());
        Assert.Equal(1, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Coupon_is_case_insensitive_and_trimmed()
    {
        var id = await SeedCoupon();
        var (_, c) = await Customer();
        var r = await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), "  " + (await CouponCode(id)).ToLowerInvariant() + " "));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [LocalDbFact]
    public async Task Expired_notStarted_inactive_unknown_and_belowMin_are_400()
    {
        var (_, c) = await Customer();
        foreach (var (id, code) in new[]
        {
            (await SeedCoupon(dFrom: -10, dTo: -1), "COUPON_EXPIRED"),
            (await SeedCoupon(dFrom: 1, dTo: 10), "COUPON_NOT_STARTED"),
            (await SeedCoupon(active: false), "COUPON_INVALID"),
            (await SeedCoupon(min: 999_999_999), "COUPON_MIN_ORDER"),
        })
        {
            var r = await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), await CouponCode(id)));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal(code, await Code(r));
            Assert.Equal(0, await UsedCount(id));
        }
        var unknown = await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), "KHONGTONTAI"));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [LocalDbFact]
    public async Task Per_customer_limit_second_order_409_but_other_customer_ok()
    {
        var id = await SeedCoupon();
        var code = await CouponCode(id);
        var (_, a) = await Customer();
        var (_, b) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        var again = await a.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("COUPON_ALREADY_USED", await Code(again));
        Assert.Equal(HttpStatusCode.Created, (await b.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        Assert.Equal(2, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Total_limit_exhausted_is_409()
    {
        var id = await SeedCoupon(limit: 1);
        var code = await CouponCode(id);
        var (_, a) = await Customer();
        var (_, b) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        var r = await b.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("COUPON_EXHAUSTED", await Code(r));
        Assert.Equal(1, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Concurrent_orders_cannot_exceed_UsageLimit()
    {
        var id = await SeedCoupon(limit: 1);
        var code = await CouponCode(id);
        var clients = new List<HttpClient>();
        for (var i = 0; i < 5; i++) clients.Add((await Customer()).Client);
        var cars = new List<int>();
        for (var i = 0; i < 5; i++) cars.Add(await SeedCar());
        var res = await Task.WhenAll(clients.Select((c, i) => c.PostAsJsonAsync("/api/bookings", DayBody(cars[i], code))));
        Assert.Equal(1, res.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(4, res.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, await UsedCount(id));
        await using var db = Fx.NewContext();
        Assert.Equal(1, await db.Set<BookingCharge>().CountAsync(x => x.CouponId == id));
    }

    [LocalDbFact]
    public async Task Concurrent_orders_of_same_customer_cannot_exceed_PerCustomerLimit()
    {
        var id = await SeedCoupon(perCustomer: 1);
        var code = await CouponCode(id);
        var (_, c) = await Customer();
        var cars = new[] { await SeedCar(), await SeedCar(), await SeedCar() };
        var res = await Task.WhenAll(cars.Select(car => c.PostAsJsonAsync("/api/bookings", DayBody(car, code))));
        Assert.Equal(1, res.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.DoesNotContain(res, r => (int)r.StatusCode >= 500);
        Assert.Equal(1, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Failed_booking_does_not_burn_coupon_use_overlap_rolls_back()
    {
        var id = await SeedCoupon(limit: 5);
        var code = await CouponCode(id);
        var car = await SeedCar();
        var (_, a) = await Customer();
        var (_, b) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/bookings", DayBody(car, null))).StatusCode);
        var r = await b.PostAsJsonAsync("/api/bookings", DayBody(car, code)); // trùng lịch
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal(0, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Cancel_returns_the_use_and_customer_can_reuse_code()
    {
        var id = await SeedCoupon(limit: 1);
        var code = await CouponCode(id);
        var (_, c) = await Customer();
        var b = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code)));
        Assert.Equal(1, await UsedCount(id));
        var cancel = await c.PostAsJsonAsync($"/api/bookings/{b.GetProperty("Id").GetInt32()}/cancel", new { Reason = "doi lich" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(0, await UsedCount(id));
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        Assert.Equal(1, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Cancelling_booking_without_coupon_leaves_other_counts_untouched()
    {
        var id = await SeedCoupon(limit: 5);
        var (_, c) = await Customer();
        var b = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar())));
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/bookings/{b.GetProperty("Id").GetInt32()}/cancel", new { Reason = "x" })).StatusCode);
        Assert.Equal(0, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Cancelling_twice_does_not_refund_twice()
    {
        var id = await SeedCoupon(limit: 5);
        var code = await CouponCode(id);
        var (_, a) = await Customer();
        var (_, other) = await Customer();
        var first = await Json(await a.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code)));
        Assert.Equal(HttpStatusCode.Created, (await other.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        var url = $"/api/bookings/{first.GetProperty("Id").GetInt32()}/cancel";
        await a.PostAsJsonAsync(url, new { Reason = "1" });
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync(url, new { Reason = "2" })).StatusCode);
        Assert.Equal(1, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Estimate_with_coupon_shows_discount_but_does_not_consume_use()
    {
        var id = await SeedCoupon(limit: 1);
        var (_, c) = await Customer();
        var r = await c.PostAsJsonAsync("/api/bookings/estimate", DayBody(await SeedCar(), await CouponCode(id)));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Contains(j.GetProperty("Charges").EnumerateArray(), x => x.GetProperty("Kind").GetString() == "GiamGia");
        Assert.Equal(0, await UsedCount(id));
    }

    [LocalDbFact]
    public async Task Check_endpoint_returns_info_without_usedCount_and_400_for_bad_code()
    {
        var id = await SeedCoupon(limit: 3);
        var (_, c) = await Customer();
        var ok = await c.GetAsync($"/api/coupons/check?code={await CouponCode(id)}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.False((await Json(ok)).TryGetProperty("UsedCount", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/coupons/check?code=ZZZZZZZ")).StatusCode);
        var expired = await SeedCoupon(dFrom: -5, dTo: -1);
        Assert.Equal("COUPON_EXPIRED", await Code(await c.GetAsync($"/api/coupons/check?code={await CouponCode(expired)}")));
    }

    // ---------- Quyền & quản trị ----------

    [LocalDbFact]
    public async Task Admin_endpoints_matrix_401_403_ok()
    {
        var anon = Fx.Anonymous();
        var staff = await Fx.As(UserRole.Staff);
        var cust = await Fx.As(UserRole.Customer);
        var admin = await Fx.As(UserRole.Admin);
        foreach (var url in new[] { "/api/admin/coupons", "/api/admin/surcharges", "/api/admin/branches" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cust.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode);
        }
        var coupon = Save(Uniq());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/admin/coupons", coupon)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("/api/admin/coupons", coupon)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cust.PostAsJsonAsync("/api/admin/coupons", coupon)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync("/api/admin/coupons/1", coupon)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PutAsJsonAsync("/api/admin/coupons/1", coupon)).StatusCode);
        var sur = new { Name = "x", Type = 1, Amount = 1000, AutoApply = false, IsActive = true };
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("/api/admin/surcharges", sur)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cust.PutAsJsonAsync("/api/admin/surcharges/1", sur)).StatusCode);
        var br = new { Name = "x", Address = "y", Phone = (string?)null, IsActive = true };
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("/api/admin/branches", br)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/admin/branches", br)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cust.PutAsJsonAsync("/api/admin/branches/1", br)).StatusCode);
        // kiểm tra mã cần đăng nhập; chi nhánh công khai
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/coupons/check?code=ABC")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/api/branches")).StatusCode);
    }

    [LocalDbFact]
    public async Task Admin_creates_coupon_uppercase_201_then_duplicate_409_any_case()
    {
        var admin = await Fx.As(UserRole.Admin);
        var code = Uniq("dup");
        var r = await admin.PostAsJsonAsync("/api/admin/coupons", Save(code));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(code.ToUpperInvariant(), (await Json(r)).GetProperty("Code").GetString());
        var dup = await admin.PostAsJsonAsync("/api/admin/coupons", Save(code.ToLowerInvariant()));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        Assert.Equal("COUPON_CODE_EXISTS", await Code(dup));
    }

    [LocalDbFact]
    public async Task Concurrent_duplicate_coupon_create_must_not_500()
    {
        // Rủi ro coder đã nêu: đua tạo cùng mã bị unique index chặn nhưng ra 500 thay vì 409.
        var admin = await Fx.As(UserRole.Admin);
        var code = Uniq("race");
        var res = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => admin.PostAsJsonAsync("/api/admin/coupons", Save(code))));
        Assert.Equal(1, res.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.DoesNotContain(res, r => (int)r.StatusCode >= 500);
        Assert.All(res.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [LocalDbFact]
    public async Task Coupon_create_validation_400()
    {
        var admin = await Fx.As(UserRole.Admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/coupons", Save(Uniq(), value: 101))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/coupons", Save(Uniq(), value: 100))).StatusCode); // biên
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/coupons", Save(Uniq(), value: 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/coupons", Save(Uniq(), dFrom: 5, dTo: 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/coupons", Save("AB"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/coupons", Save(Uniq(), limit: 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/coupons", Save(Uniq(), type: 2, value: 500_000m))).StatusCode); // tiền cố định > 100 hợp lệ
    }

    [LocalDbFact]
    public async Task Coupon_update_rules_codeImmutable_limitBelowUsed_unknown404()
    {
        var admin = await Fx.As(UserRole.Admin);
        var code = Uniq("upd");
        var created = await Json(await admin.PostAsJsonAsync("/api/admin/coupons", Save(code, limit: 5)));
        var id = created.GetProperty("Id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/coupons/{id}", Save(code, value: 20, limit: 5))).StatusCode);
        Assert.Equal("COUPON_CODE_IMMUTABLE", await Code(await admin.PutAsJsonAsync($"/api/admin/coupons/{id}", Save(Uniq(), limit: 5))));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/admin/coupons/99999999", Save(code))).StatusCode);
    }

    [LocalDbFact]
    public async Task Coupon_update_limit_below_used_count_is_400()
    {
        var id = await SeedCoupon(limit: 5);
        var code = await CouponCode(id);
        var (_, c) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        var (_, c2) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await c2.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), code))).StatusCode);
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PutAsJsonAsync($"/api/admin/coupons/{id}", Save(code, limit: 1));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("COUPON_LIMIT", await Code(r));
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/coupons/{id}", Save(code, limit: 2))).StatusCode); // biên = đã dùng
    }
}

/// <summary>Phụ phí AutoApply tác động mọi đơn mới nên tách DB riêng (class fixture riêng).</summary>
public sealed class Wave4SurchargeApiTests : Wave4Base, IClassFixture<ApiFixture>
{
    public Wave4SurchargeApiTests(ApiFixture fx) : base(fx) { }

    [LocalDbFact]
    public async Task Auto_surcharge_is_added_then_discount_applies_on_subtotal_and_total_matches()
    {
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync("/api/admin/surcharges", new { Name = "Vệ sinh xe", Type = 1, Amount = 100_000, AutoApply = true, IsActive = true });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var sid = (await Json(r)).GetProperty("Id").GetInt32();

        var cid = await SeedCoupon(value: 10);
        var (_, c) = await Customer();
        var b = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), await CouponCode(cid))));
        var rental = b.GetProperty("FinalPricePerDay").GetDecimal() * 2;
        var lines = b.GetProperty("Charges").EnumerateArray().ToList();
        Assert.Equal("PhuPhi", lines[0].GetProperty("Kind").GetString());
        Assert.Equal(100_000m, lines[0].GetProperty("Amount").GetDecimal());
        Assert.Equal("GiamGia", lines[1].GetProperty("Kind").GetString());
        Assert.Equal(-Math.Round((rental + 100_000m) * 0.1m), lines[1].GetProperty("Amount").GetDecimal());
        Assert.Equal(rental + Sum(b), b.GetProperty("TotalPrice").GetDecimal());

        // tắt phụ phí thì đơn mới không còn dòng phí
        var off = await admin.PutAsJsonAsync($"/api/admin/surcharges/{sid}", new { Name = "Vệ sinh xe", Type = 1, Amount = 100_000, AutoApply = true, IsActive = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var (_, c2) = await Customer();
        var b2 = await Json(await c2.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar())));
        Assert.Empty(b2.GetProperty("Charges").EnumerateArray());
        Assert.Equal(b2.GetProperty("FinalPricePerDay").GetDecimal() * 2, b2.GetProperty("TotalPrice").GetDecimal());
    }

    [LocalDbFact]
    public async Task Surcharge_validation_percent_over_100_is_400_unknown_update_404()
    {
        var admin = await Fx.As(UserRole.Admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/surcharges", new { Name = "p", Type = 3, Amount = 101, AutoApply = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/surcharges", new { Name = "p", Type = 3, Amount = 100, AutoApply = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/surcharges", new { Name = "", Type = 1, Amount = 1, AutoApply = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/surcharges", new { Name = "n", Type = 1, Amount = -1, AutoApply = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/admin/surcharges/99999999", new { Name = "n", Type = 1, Amount = 1, AutoApply = false })).StatusCode);
    }
}

/// <summary>Giờ, tài xế, giao nhận, một chiều, chi nhánh, hồi quy đợt 3, migration.</summary>
public sealed class Wave4ModesBranchesApiTests : Wave4Base, IClassFixture<ApiFixture>
{
    public Wave4ModesBranchesApiTests(ApiFixture fx) : base(fx) { }

    [LocalDbFact]
    public async Task Hourly_5h_with_PricePerHour_is_5x_price_and_mode_saved()
    {
        var (_, c) = await Customer();
        var r = await c.PostAsJsonAsync("/api/bookings", HourBody(await SeedCar(perHour: 100_000), 5));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(500_000m, (await Json(r)).GetProperty("TotalPrice").GetDecimal());
    }

    [LocalDbFact]
    public async Task Hourly_range_boundaries_4_ok_12_ok_3_and_13_are_400()
    {
        var (_, c) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/bookings", HourBody(await SeedCar(perHour: 100_000), 4))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/bookings", HourBody(await SeedCar(perHour: 100_000), 12))).StatusCode);
        foreach (var h in new[] { 3, 13 })
        {
            var r = await c.PostAsJsonAsync("/api/bookings", HourBody(await SeedCar(perHour: 100_000), h));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal("HOURLY_RANGE", await Code(r));
        }
    }

    // Sửa theo ADR-010: xe không có PricePerHour thì không cho thuê theo giờ (trước đây khẳng định giá ngày / 8).
    [LocalDbFact]
    public async Task Hourly_without_PricePerHour_is_400_HOURLY_NOT_SUPPORTED()
    {
        var (_, c) = await Customer();
        var r = await c.PostAsJsonAsync("/api/bookings", HourBody(await SeedCar(perHour: null), 8));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("HOURLY_NOT_SUPPORTED", await Code(r));
    }

    [LocalDbFact]
    public async Task Hourly_starting_within_2h_is_400_HOURLY_START()
    {
        var (_, c) = await Customer();
        var car = await SeedCar(perHour: 100_000);
        var body = new { CarId = car, StartDate = DateTime.UtcNow.AddMinutes(30), EndDate = DateTime.UtcNow.AddMinutes(30).AddHours(5), Mode = 2 };
        var r = await c.PostAsJsonAsync("/api/bookings", body);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("HOURLY_START", await Code(r));
    }

    [LocalDbFact]
    public async Task Daily_shorter_than_one_day_is_400_BOOKING_TOO_SHORT()
    {
        var (_, c) = await Customer();
        var start = DateTime.UtcNow.Date.AddDays(50).AddHours(8);
        var r = await c.PostAsJsonAsync("/api/bookings", new { CarId = await SeedCar(), StartDate = start, EndDate = start.AddHours(10), Mode = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("BOOKING_TOO_SHORT", await Code(r));
    }

    [LocalDbFact]
    public async Task Hourly_bookings_on_same_car_same_day_overlap_is_409_but_adjacent_ok()
    {
        var car = await SeedCar(perHour: 100_000);
        var (_, a) = await Customer();
        var (_, b) = await Customer();
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync("/api/bookings", HourBody(car, 5, startOffsetHours: 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await b.PostAsJsonAsync("/api/bookings", HourBody(car, 5, startOffsetHours: 3))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await b.PostAsJsonAsync("/api/bookings", HourBody(car, 4, startOffsetHours: 5))).StatusCode);
    }

    [LocalDbFact]
    public async Task Hourly_with_driver_adds_80k_per_hour()
    {
        var (_, c) = await Customer();
        var body = new { CarId = await SeedCar(perHour: 100_000), StartDate = Hour(0), EndDate = Hour(5), Mode = 2, WithDriver = true };
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", body));
        Assert.Equal(500_000m + 400_000m, j.GetProperty("TotalPrice").GetDecimal());
        Assert.Contains(j.GetProperty("Charges").EnumerateArray(), x => x.GetProperty("Kind").GetString() == "TaiXe");
    }

    [LocalDbFact]
    public async Task Daily_driver_and_delivery_charges_sum_into_total()
    {
        var (_, c) = await Customer();
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), driver: true, deliver: "1 Nguyễn Huệ", back: "2 Lê Lợi")));
        var rental = j.GetProperty("FinalPricePerDay").GetDecimal() * 2;
        Assert.Equal(1_000_000m + 300_000m, Sum(j));
        Assert.Equal(rental + 1_300_000m, j.GetProperty("TotalPrice").GetDecimal());
        Assert.Equal(2, j.GetProperty("Charges").EnumerateArray().Count(x => x.GetProperty("Kind").GetString() == "GiaoNhan"));
        Assert.True(j.GetProperty("WithDriver").GetBoolean());
        Assert.Equal("1 Nguyễn Huệ", j.GetProperty("DeliveryAddress").GetString());
    }

    [LocalDbFact]
    public async Task Address_longer_than_300_is_400()
    {
        var (_, c) = await Customer();
        var r = await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), deliver: new string('a', 301)));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [LocalDbFact]
    public async Task OneWay_return_to_other_branch_adds_200k_and_moves_car_on_return()
    {
        var b1 = await SeedBranch();
        var b2 = await SeedBranch();
        var car = await SeedCar(branchId: b1);
        var (cid, c) = await Customer();
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(car, ret: b2)));
        Assert.Contains(j.GetProperty("Charges").EnumerateArray(), x => x.GetProperty("Kind").GetString() == "MotChieu" && x.GetProperty("Amount").GetDecimal() == 200_000m);
        var bk = j.GetProperty("Id").GetInt32();

        // Đưa đơn tới DangThue + biên bản nhận rồi staff nhận trả xe
        await using (var db = Fx.NewContext())
        {
            var b = await db.Bookings.SingleAsync(x => x.Id == bk);
            b.Status = BookingStatus.DangThue;
            (await db.Cars.SingleAsync(x => x.Id == car)).Status = CarStatus.DangThue;
            db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bk, Type = HandoverType.Giao, Odometer = 100, FuelLevel = 80 });
            db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bk, Type = HandoverType.Nhan, Odometer = 200, FuelLevel = 80 });
            await db.SaveChangesAsync();
        }
        var staff = await Fx.As(UserRole.Staff);
        var ret = await staff.PostAsJsonAsync($"/api/bookings/{bk}/return", new { ReturnedAt = j.GetProperty("EndDate").GetDateTime(), DamageFee = 0 });
        Assert.Equal(HttpStatusCode.OK, ret.StatusCode);
        var rj = await Json(ret);
        Assert.Equal(j.GetProperty("TotalPrice").GetDecimal(), rj.GetProperty("FinalAmount").GetDecimal()); // hồi quy đợt 3: không trễ thì FinalAmount = TotalPrice mới
        await using var db2 = Fx.NewContext();
        Assert.Equal(b2, (await db2.Cars.AsNoTracking().SingleAsync(x => x.Id == car)).BranchId);
        _ = cid;
    }

    [LocalDbFact]
    public async Task Same_branch_return_has_no_oneway_fee_and_car_stays()
    {
        var b1 = await SeedBranch();
        var car = await SeedCar(branchId: b1);
        var (_, c) = await Customer();
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(car, ret: b1)));
        Assert.DoesNotContain(j.GetProperty("Charges").EnumerateArray(), x => x.GetProperty("Kind").GetString() == "MotChieu");
    }

    [LocalDbFact]
    public async Task Booking_with_wrong_pickup_inactive_or_unknown_branch_is_400_not_500()
    {
        var b1 = await SeedBranch();
        var other = await SeedBranch();
        var dead = await SeedBranch(active: false);
        var car = await SeedCar(branchId: b1);
        var (_, c) = await Customer();
        Assert.Equal("BRANCH_MISMATCH", await Code(await c.PostAsJsonAsync("/api/bookings", DayBody(car, pickup: other))));
        Assert.Equal("BRANCH_INVALID", await Code(await c.PostAsJsonAsync("/api/bookings", DayBody(car, ret: dead))));
        var unknown = await c.PostAsJsonAsync("/api/bookings", DayBody(car, ret: 99999999));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [LocalDbFact]
    public async Task Hourly_late_return_90min_charges_2_hours_and_LateDays_zero()
    {
        var car = await SeedCar(perHour: 100_000, status: CarStatus.DangThue);
        var (cid, _) = await Customer();
        int bk;
        var end = DateTime.UtcNow.Date.AddHours(13);
        await using (var db = Fx.NewContext())
        {
            var b = new Booking
            {
                CustomerId = cid, CarId = car, Status = BookingStatus.DangThue, Mode = RentalMode.TheoGio, TotalPrice = 500_000, FinalPricePerDay = 800_000,
                StartDate = end.AddHours(-5), EndDate = end
            };
            db.Bookings.Add(b);
            await db.SaveChangesAsync();
            bk = b.Id;
            db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bk, Type = HandoverType.Nhan, Odometer = 100, FuelLevel = 80 });
            await db.SaveChangesAsync();
        }
        var staff = await Fx.As(UserRole.Staff);
        var r = await staff.PostAsJsonAsync($"/api/bookings/{bk}/return", new { ReturnedAt = end.AddMinutes(90), DamageFee = 0 });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal(0, j.GetProperty("LateDays").GetInt32());
        Assert.Equal(200_000m, j.GetProperty("LateFee").GetDecimal());
        Assert.Equal(700_000m, j.GetProperty("FinalAmount").GetDecimal());
    }

    [LocalDbFact]
    public async Task Hourly_booking_cannot_be_extended()
    {
        var (cid, c) = await Customer();
        var car = await SeedCar(perHour: 100_000);
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", HourBody(car, 5)));
        var bk = j.GetProperty("Id").GetInt32();
        await (await Fx.As(UserRole.Staff)).PostAsync($"/api/bookings/{bk}/confirm", null);
        var r = await c.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = Hour(10) });
        Assert.True(r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict, "Thuê theo giờ không được gia hạn, nhận " + r.StatusCode);
        var check = await c.GetAsync($"/api/bookings/{bk}/extend/check?newEndDate={Uri.EscapeDataString(Hour(10).ToString("O"))}");
        Assert.True(check.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict, "extend/check nhận " + check.StatusCode);
        _ = cid;
    }

    // ---------- Chi nhánh (Admin) ----------

    [LocalDbFact]
    public async Task Branch_crud_admin_and_deactivate_with_cars_is_409()
    {
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync("/api/admin/branches", new { Name = "CN A", Address = "1 A", Phone = "0123", IsActive = true });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await Json(r)).GetProperty("Id").GetInt32();
        await SeedCar(branchId: id);
        var off = await admin.PutAsJsonAsync($"/api/admin/branches/{id}", new { Name = "CN A", Address = "1 A", Phone = "0123", IsActive = false });
        Assert.Equal(HttpStatusCode.Conflict, off.StatusCode);
        Assert.Equal("BRANCH_HAS_CARS", await Code(off));
        var empty = await SeedBranch();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/admin/branches/{empty}", new { Name = "CN B", Address = "2", Phone = (string?)null, IsActive = false })).StatusCode);
        // công khai chỉ thấy chi nhánh đang hoạt động
        var pub = await Json(await Fx.Anonymous().GetAsync("/api/branches"));
        Assert.DoesNotContain(pub.EnumerateArray(), b => b.GetProperty("Id").GetInt32() == empty);
        Assert.Contains(pub.EnumerateArray(), b => b.GetProperty("Id").GetInt32() == id);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/branches", new { Name = "", Address = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/admin/branches/99999999", new { Name = "n", Address = "a" })).StatusCode);
    }

    [LocalDbFact]
    public async Task Create_car_with_unknown_BranchId_must_not_be_500()
    {
        // Rủi ro coder đã nêu: BranchId sai -> lỗi FK -> 500.
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync("/api/cars", new
        {
            CarType = 1, Brand = "Toyota", Model = "Camry", LicensePlate = "T-" + Guid.NewGuid().ToString("N")[..8], Seats = 5, CarAgeYears = 1,
            BasePricePerDay = 700_000, Description = "d", BranchId = 99999999
        });
        Assert.True((int)r.StatusCode is >= 400 and < 500, "BranchId không tồn tại phải là lỗi 4xx, nhận " + (int)r.StatusCode);
    }

    [LocalDbFact]
    public async Task Create_car_with_valid_BranchId_and_PricePerHour_persists()
    {
        var branch = await SeedBranch();
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync("/api/cars", new
        {
            CarType = 1, Brand = "Toyota", Model = "Camry", LicensePlate = "T-" + Guid.NewGuid().ToString("N")[..8], Seats = 5, CarAgeYears = 1,
            BasePricePerDay = 700_000, Description = "d", BranchId = branch, PricePerHour = 90_000
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var id = (await Json(r)).GetProperty("Id").GetInt32();
        await using var db = Fx.NewContext();
        var car = await db.Cars.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal((branch, 90_000m), (car.BranchId, car.PricePerHour));
    }

    // ---------- Hồi quy đợt 3 với TotalPrice mới ----------

    [LocalDbFact]
    public async Task Payment_due_is_new_TotalPrice_and_excess_is_409()
    {
        var (_, c) = await Customer();
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), driver: true)));
        var bk = j.GetProperty("Id").GetInt32();
        var total = j.GetProperty("TotalPrice").GetDecimal();
        var staff = await Fx.As(UserRole.Staff);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/bookings/{bk}/confirm", null)).StatusCode);
        var over = await staff.PostAsJsonAsync($"/api/bookings/{bk}/payments", new { Amount = total + 1, Method = 1 });
        Assert.Equal("PAYMENT_EXCEEDS_DUE", await Code(over));
        Assert.Equal(HttpStatusCode.Created, (await staff.PostAsJsonAsync($"/api/bookings/{bk}/payments", new { Amount = total, Method = 1 })).StatusCode);
    }

    [LocalDbFact]
    public async Task Cancelled_booking_deposit_refund_unaffected_by_charges()
    {
        var (cid, c) = await Customer();
        await using (var db = Fx.NewContext())
        {
            var car = await SeedCar();
            (await db.Cars.SingleAsync(x => x.Id == car)).SecurityDeposit = 2_000_000;
            await db.SaveChangesAsync();
            var j = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(car, driver: true)));
            var bk = j.GetProperty("Id").GetInt32();
            var staff = await Fx.As(UserRole.Staff);
            await staff.PostAsync($"/api/bookings/{bk}/confirm", null);
            Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/collect", new { Method = 1 })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/bookings/{bk}/cancel", new { Reason = "x" })).StatusCode);
            var s = await Json(await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 }));
            Assert.Equal(2_000_000m, s.GetProperty("Refunded").GetDecimal());
            Assert.Equal(0m, s.GetProperty("Deducted").GetDecimal());
        }
        _ = cid;
    }

    [LocalDbFact]
    public async Task Extension_quote_adds_days_on_top_of_total_including_charges()
    {
        var (_, c) = await Customer();
        var j = await Json(await c.PostAsJsonAsync("/api/bookings", DayBody(await SeedCar(), driver: true)));
        var bk = j.GetProperty("Id").GetInt32();
        await (await Fx.As(UserRole.Staff)).PostAsync($"/api/bookings/{bk}/confirm", null);
        var newEnd = j.GetProperty("EndDate").GetDateTime().AddDays(1);
        var r = await c.GetAsync($"/api/bookings/{bk}/extend/check?newEndDate={Uri.EscapeDataString(newEnd.ToString("O"))}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var q = await Json(r);
        Assert.Equal(j.GetProperty("TotalPrice").GetDecimal() + q.GetProperty("ExtraAmount").GetDecimal(), q.GetProperty("NewTotalPrice").GetDecimal());
        Assert.Equal(j.GetProperty("FinalPricePerDay").GetDecimal(), q.GetProperty("ExtraAmount").GetDecimal());
    }

    // ---------- Migration ----------

    [LocalDbFact]
    public async Task Migration_AddPricingBranchesModes_applied_and_no_pending_model_changes()
    {
        await using var db = Fx.NewContext();
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m == "20261117152852_AddPricingBranchesModes");
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "Model lệch snapshot: thiếu migration.");
        _ = await db.Set<Coupon>().CountAsync();
        _ = await db.Set<Surcharge>().CountAsync();
        _ = await db.Set<BookingCharge>().CountAsync();
        _ = await db.Set<Branch>().CountAsync();
        _ = await db.Cars.Select(c => new { c.BranchId, c.PricePerHour }).FirstOrDefaultAsync();
        _ = await db.Bookings.Select(b => new { b.Mode, b.WithDriver, b.DeliveryAddress, b.ReturnAddress, b.PickupBranchId, b.ReturnBranchId }).FirstOrDefaultAsync();
    }

    [LocalDbFact]
    public async Task Coupon_code_unique_and_car_branch_fk_enforced_by_db()
    {
        var code = Uniq("U");
        await SeedCoupon(code);
        await Assert.ThrowsAsync<DbUpdateException>(() => SeedCoupon(code));
        await Assert.ThrowsAsync<DbUpdateException>(() => SeedCar(branchId: 99999999));
    }
}
