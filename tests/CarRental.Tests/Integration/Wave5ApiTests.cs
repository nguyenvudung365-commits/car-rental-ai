using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Branches;
using CarRental.Domain.Modules.Cars;
using CarRental.Tests.Support;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

public abstract class Wave5Base
{
    protected readonly ApiFixture Fx;
    protected Wave5Base(ApiFixture fx) => Fx = fx;

    protected static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    protected static async Task<string?> Code(HttpResponseMessage r)
    {
        var j = await Json(r);
        return j.TryGetProperty("code", out var c) ? c.GetString() : null;
    }

    protected static string Uniq(string p = "W5") => p + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    protected async Task<int> SeedBranch()
    {
        await using var db = Fx.NewContext();
        var b = new Branch { Name = Uniq("B"), Address = "addr", IsActive = true };
        db.Set<Branch>().Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    protected async Task<int> Car(CarStatus st = CarStatus.SanSang, int? branch = null)
    {
        await using var db = Fx.NewContext();
        var c = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = Uniq("51A-"), Seats = 5, CarAgeYears = 1,
            BasePricePerDay = 700_000, Status = st, BranchId = branch
        };
        db.Cars.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    protected async Task<CarStatus> StatusOf(int carId)
    {
        await using var db = Fx.NewContext();
        return (await db.Cars.AsNoTracking().SingleAsync(c => c.Id == carId)).Status;
    }

    protected async Task SetStatus(int carId, CarStatus st)
    {
        await using var db = Fx.NewContext();
        (await db.Cars.SingleAsync(c => c.Id == carId)).Status = st;
        await db.SaveChangesAsync();
    }

    protected async Task<int> Booking(int carId, DateTime start, DateTime end, BookingStatus st = BookingStatus.DaXacNhan,
        RentalMode mode = RentalMode.TheoNgay, int? customerId = null)
    {
        customerId ??= (await Fx.SeedUser(UserRole.Customer)).Id;
        await using var db = Fx.NewContext();
        var b = new Booking
        {
            CustomerId = customerId.Value, CarId = carId, StartDate = start, EndDate = end, FinalPricePerDay = 700_000, TotalPrice = 700_000,
            Status = st, Mode = mode, CorrelationId = Guid.NewGuid().ToString()
        };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    protected async Task<int> Plan(int carId, MaintenanceType type = MaintenanceType.BaoDuong, int dueInDays = 0, int? km = null,
        MaintenanceStatus st = MaintenanceStatus.KeHoach, decimal? cost = null)
    {
        await using var db = Fx.NewContext();
        var m = new CarMaintenance { CarId = carId, Type = type, DueDate = DateTime.UtcNow.Date.AddDays(dueInDays), DueOdometer = km, Status = st, Cost = cost };
        db.Set<CarMaintenance>().Add(m);
        await db.SaveChangesAsync();
        return m.Id;
    }

    protected static string Q(DateTime d) => Uri.EscapeDataString(d.ToString("o"));
}

/// <summary>Ma trận quyền HTTP cho các route đợt 5: 401 ẩn danh, 403 Customer, Staff/Admin đúng.</summary>
public sealed class Wave5AuthMatrixTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5AuthMatrixTests(ApiFixture fx) : base(fx) { }

    private static string Cal => $"/api/cars/calendar?from={Q(DateTime.UtcNow.Date)}&to={Q(DateTime.UtcNow.Date.AddDays(7))}";

    // (method, url, staffAllowed)
    public static IEnumerable<object[]> Routes()
    {
        yield return ["GET", "/api/maintenance", true];
        yield return ["GET", "/api/maintenance/due", true];
        yield return ["POST", "/api/maintenance", true];
        yield return ["POST", "/api/maintenance/999999/start", true];
        yield return ["POST", "/api/maintenance/999999/complete", true];
        yield return ["POST", "/api/maintenance/999999/cancel", true];
        yield return ["GET", "CAL", true];
        yield return ["GET", "/api/dashboard/summary", true];
        yield return ["GET", "/api/cars/import/template", true];
        yield return ["POST", "/api/cars/import", true];
        yield return ["GET", "/api/bookings/export", true];
        yield return ["GET", "/api/reports/revenue/export", false];
        yield return ["GET", "/api/reports/occupancy/export", false];
    }

    private static Task<HttpResponseMessage> Send(HttpClient c, string method, string url)
        => c.SendAsync(new HttpRequestMessage(new HttpMethod(method), url == "CAL" ? Cal : url)
        { Content = method != "POST" ? null : url == "/api/cars/import" ? new MultipartFormDataContent { { new ByteArrayContent([1]), "file", "x.xlsx" } } : JsonContent.Create(new { }) });

    [LocalDbTheory]
    [MemberData(nameof(Routes))]
    public async Task Anonymous_Is401(string method, string url, bool _)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(Fx.Anonymous(), method, url)).StatusCode);
    }

    [LocalDbTheory]
    [MemberData(nameof(Routes))]
    public async Task Customer_Is403(string method, string url, bool _)
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(await Fx.As(UserRole.Customer), method, url)).StatusCode);
    }

    [LocalDbTheory]
    [MemberData(nameof(Routes))]
    public async Task Staff_AllowedOrForbidden_AsDocumented(string method, string url, bool staffAllowed)
    {
        var s = (await Send(await Fx.As(UserRole.Staff), method, url)).StatusCode;
        if (staffAllowed) Assert.True(s != HttpStatusCode.Unauthorized && s != HttpStatusCode.Forbidden, "Staff bị chặn: " + s);
        else Assert.Equal(HttpStatusCode.Forbidden, s);
    }

    [LocalDbTheory]
    [MemberData(nameof(Routes))]
    public async Task Admin_IsNeverBlocked(string method, string url, bool _)
    {
        var s = (await Send(await Fx.As(UserRole.Admin), method, url)).StatusCode;
        Assert.True(s != HttpStatusCode.Unauthorized && s != HttpStatusCode.Forbidden, "Admin bị chặn: " + s);
    }
}

/// <summary>[Theory] cần LocalDB; bỏ qua khi không có.</summary>
public sealed class LocalDbTheoryAttribute : TheoryAttribute
{
    public LocalDbTheoryAttribute()
    {
        if (new LocalDbFactAttribute().Skip is { } s) Skip = s;
    }
}

public sealed class Wave5MaintenanceApiTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5MaintenanceApiTests(ApiFixture fx) : base(fx) { }

    private static object Empty => new { };

    [LocalDbFact]
    public async Task Create_ThenStart_SetsCarBaoTri_AndComplete_ReturnsSanSang()
    {
        var car = await Car();
        var staff = await Fx.As(UserRole.Staff);
        var r = await staff.PostAsJsonAsync("/api/maintenance", new { CarId = car, Type = 1, DueDate = DateTime.UtcNow.Date, Note = "n" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await Json(r)).GetProperty("Id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/api/maintenance/{id}/start", Empty)).StatusCode);
        Assert.Equal(CarStatus.BaoTri, await StatusOf(car));

        var done = await staff.PostAsJsonAsync($"/api/maintenance/{id}/complete", new { Cost = 250_000, NextDueDate = DateTime.UtcNow.Date.AddDays(180) });
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal(CarStatus.SanSang, await StatusOf(car));
        // Staff không thấy chi phí
        Assert.Equal(JsonValueKind.Null, (await Json(done)).GetProperty("Cost").ValueKind);
        // sinh lịch kế tiếp
        var list = await Json(await staff.GetAsync($"/api/maintenance?carId={car}&status=1"));
        Assert.Equal(1, list.GetArrayLength());
    }

    [LocalDbFact]
    public async Task Start_CarRenting_Is409_CAR_IN_USE_EvenWithForce()
    {
        var car = await Car(CarStatus.DangThue);
        var id = await Plan(car);
        var r = await (await Fx.As(UserRole.Admin)).PostAsJsonAsync($"/api/maintenance/{id}/start", new { Force = true });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("CAR_IN_USE", await Code(r));
        Assert.Equal(CarStatus.DangThue, await StatusOf(car));
    }

    [LocalDbFact]
    public async Task Start_OverlappingBooking_409_ThenForcePasses()
    {
        var car = await Car();
        await Booking(car, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(2), BookingStatus.ChoXacNhan);
        var id = await Plan(car);
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync($"/api/maintenance/{id}/start", Empty);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("MAINTENANCE_BOOKING_OVERLAP", await Code(r));
        Assert.Equal(CarStatus.SanSang, await StatusOf(car));

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/maintenance/{id}/start", new { Force = true })).StatusCode);
        Assert.Equal(CarStatus.BaoTri, await StatusOf(car));
    }

    [LocalDbFact]
    public async Task Start_CancelledBooking_DoesNotBlock()
    {
        var car = await Car();
        await Booking(car, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(2), BookingStatus.DaHuy);
        var id = await Plan(car);
        Assert.Equal(HttpStatusCode.OK, (await (await Fx.As(UserRole.Staff)).PostAsJsonAsync($"/api/maintenance/{id}/start", Empty)).StatusCode);
    }

    [LocalDbFact]
    public async Task Complete_DoesNotOverwrite_NgungHoatDong()
    {
        var car = await Car();
        var id = await Plan(car);
        var admin = await Fx.As(UserRole.Admin);
        await admin.PostAsJsonAsync($"/api/maintenance/{id}/start", Empty);
        await SetStatus(car, CarStatus.NgungHoatDong);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/maintenance/{id}/complete", Empty)).StatusCode);
        Assert.Equal(CarStatus.NgungHoatDong, await StatusOf(car));
    }

    [LocalDbFact]
    public async Task Complete_KeepsBaoTri_WhileAnotherScheduleInProgress()
    {
        var car = await Car();
        var a = await Plan(car);
        var b = await Plan(car, MaintenanceType.DangKiem);
        var admin = await Fx.As(UserRole.Admin);
        await admin.PostAsJsonAsync($"/api/maintenance/{a}/start", Empty);
        await admin.PostAsJsonAsync($"/api/maintenance/{b}/start", Empty); // xe đã BaoTri vẫn được bắt đầu
        await admin.PostAsJsonAsync($"/api/maintenance/{a}/complete", Empty);
        Assert.Equal(CarStatus.BaoTri, await StatusOf(car));
        await admin.PostAsJsonAsync($"/api/maintenance/{b}/complete", Empty);
        Assert.Equal(CarStatus.SanSang, await StatusOf(car));
    }

    [LocalDbFact]
    public async Task StateErrors_AndNotFound()
    {
        var car = await Car();
        var done = await Plan(car, st: MaintenanceStatus.HoanThanh);
        var admin = await Fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync($"/api/maintenance/{done}/start", Empty);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("MAINTENANCE_STATE", await Code(r));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/maintenance/{done}/cancel", Empty)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/api/maintenance/99999999/start", Empty)).StatusCode);
    }

    [LocalDbFact]
    public async Task Cost_VisibleToAdminOnly_InList()
    {
        var car = await Car();
        await Plan(car, cost: 777_000);
        var a = await Json(await (await Fx.As(UserRole.Admin)).GetAsync($"/api/maintenance?carId={car}"));
        var s = await Json(await (await Fx.As(UserRole.Staff)).GetAsync($"/api/maintenance?carId={car}"));
        Assert.Equal(777_000, a[0].GetProperty("Cost").GetDecimal());
        Assert.Equal(JsonValueKind.Null, s[0].GetProperty("Cost").ValueKind);
    }

    [LocalDbFact]
    public async Task Create_Validation()
    {
        var staff = await Fx.As(UserRole.Staff);
        var car = await Car();
        Assert.Equal(HttpStatusCode.NotFound, (await staff.PostAsJsonAsync("/api/maintenance", new { CarId = 99999999, Type = 1, DueDate = DateTime.UtcNow })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/maintenance", new { CarId = car, Type = 9, DueDate = DateTime.UtcNow })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/maintenance", new { CarId = car, Type = 1, DueDate = DateTime.UtcNow, DueOdometer = -1 })).StatusCode);
    }

    [LocalDbFact]
    public async Task Due_ListsOverdueUpcomingAndKm_FromRealHandoverData()
    {
        var car = await Car();
        var over = await Plan(car, MaintenanceType.DangKiem, dueInDays: -3);
        var soon = await Plan(car, MaintenanceType.BaoHiem, dueInDays: 5);
        var far = await Plan(car, MaintenanceType.BaoHiem, dueInDays: 100);
        var km = await Plan(car, MaintenanceType.BaoDuong, dueInDays: 200, km: 10_000);
        var bk = await Booking(car, DateTime.UtcNow.Date.AddDays(-10), DateTime.UtcNow.Date.AddDays(-8), BookingStatus.DaTraXe);
        await using (var db = Fx.NewContext())
        {
            db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bk, Type = HandoverType.Giao, Odometer = 9_700, FuelLevel = 50 });
            await db.SaveChangesAsync();
        }
        var j = await Json(await (await Fx.As(UserRole.Staff)).GetAsync("/api/maintenance/due?withinDays=30"));
        int[] Ids(string k) => j.GetProperty(k).EnumerateArray().Select(x => x.GetProperty("Id").GetInt32()).ToArray();
        Assert.Contains(over, Ids("Overdue"));
        Assert.DoesNotContain(soon, Ids("Overdue"));
        Assert.Contains(soon, Ids("Upcoming"));
        Assert.DoesNotContain(far, Ids("Upcoming"));
        Assert.Contains(km, Ids("ByOdometer"));
        Assert.Equal(HttpStatusCode.BadRequest, (await (await Fx.As(UserRole.Staff)).GetAsync("/api/maintenance/due?withinDays=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await (await Fx.As(UserRole.Staff)).GetAsync("/api/maintenance/due?withinDays=366")).StatusCode);
    }

    [LocalDbFact]
    public async Task Start_Concurrent_OnlyOneWins()
    {
        var car = await Car();
        var id = await Plan(car);
        var admin = await Fx.As(UserRole.Admin);
        var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => admin.PostAsJsonAsync($"/api/maintenance/{id}/start", Empty)));
        Assert.Equal(1, rs.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.DoesNotContain(rs, r => (int)r.StatusCode >= 500);
    }
}

public sealed class Wave5CalendarApiTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5CalendarApiTests(ApiFixture fx) : base(fx) { }

    private async Task<JsonElement> Get(int branch, DateTime from, DateTime to)
    {
        var r = await (await Fx.As(UserRole.Staff)).GetAsync($"/api/cars/calendar?from={Q(from)}&to={Q(to)}&branchId={branch}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await Json(r);
    }

    [LocalDbFact]
    public async Task Calendar_ShowsDayAndHourBookings_ExcludesCancelled_FiltersBranch()
    {
        var br = await SeedBranch();
        var car = await Car(branch: br);
        await Car(branch: await SeedBranch()); // xe chi nhánh khác
        var other = await Car(CarStatus.ChoDuyet, br); // chờ duyệt: ẩn
        var d = DateTime.UtcNow.Date.AddDays(10);
        var day = await Booking(car, d, d.AddDays(2));
        var hour = await Booking(car, d.AddDays(5), d.AddDays(5).AddHours(6), mode: RentalMode.TheoGio); // bắt đầu đúng 00:00 UTC
        var cancelled = await Booking(car, d.AddDays(3), d.AddDays(4), BookingStatus.DaHuy);
        var m = await Plan(car, dueInDays: 11);

        var j = await Get(br, d.AddDays(-1), d.AddDays(20));
        Assert.Equal(1, j.GetArrayLength());
        Assert.NotEqual(other, j[0].GetProperty("CarId").GetInt32());
        var items = j[0].GetProperty("Items").EnumerateArray().ToList();
        var ids = items.Where(i => i.GetProperty("Kind").GetString() == "Booking").Select(i => i.GetProperty("Id").GetInt32()).ToList();
        Assert.Contains(day, ids);
        Assert.Contains(hour, ids);
        Assert.DoesNotContain(cancelled, ids);
        Assert.Contains(items, i => i.GetProperty("Kind").GetString() == "Maintenance" && i.GetProperty("Id").GetInt32() == m);

        // đơn giờ giữ nguyên mốc UTC, không bị nới thành cả ngày
        var h = items.Single(i => i.GetProperty("Id").GetInt32() == hour && i.GetProperty("Kind").GetString() == "Booking");
        Assert.Equal(d.AddDays(5).ToString("o").TrimEnd('Z'), h.GetProperty("Start").GetDateTime().ToString("o").TrimEnd('Z'));
        Assert.Equal(d.AddDays(5).AddHours(6).ToString("o").TrimEnd('Z'), h.GetProperty("End").GetDateTime().ToString("o").TrimEnd('Z'));
    }

    [LocalDbFact]
    public async Task Calendar_WindowBoundaries_AreHalfOpen()
    {
        var br = await SeedBranch();
        var car = await Car(branch: br);
        var d = DateTime.UtcNow.Date.AddDays(30);
        var hour = await Booking(car, d.AddHours(2), d.AddHours(6), mode: RentalMode.TheoGio);
        bool Has(JsonElement j) => j[0].GetProperty("Items").EnumerateArray().Any(i => i.GetProperty("Id").GetInt32() == hour && i.GetProperty("Kind").GetString() == "Booking");
        Assert.False(Has(await Get(br, d.AddHours(6), d.AddDays(1)))); // cửa sổ bắt đầu đúng lúc đơn kết thúc
        Assert.False(Has(await Get(br, d.AddDays(-1), d.AddHours(2)))); // cửa sổ kết thúc đúng lúc đơn bắt đầu
        Assert.True(Has(await Get(br, d.AddHours(5), d.AddDays(1))));
    }

    [LocalDbFact]
    public async Task Calendar_RangeValidation()
    {
        var s = await Fx.As(UserRole.Staff);
        var d = DateTime.UtcNow.Date;
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/cars/calendar?from={Q(d)}&to={Q(d)}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await s.GetAsync($"/api/cars/calendar?from={Q(d)}&to={Q(d.AddDays(63))}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await s.GetAsync($"/api/cars/calendar?from={Q(d)}&to={Q(d.AddDays(62))}")).StatusCode);
    }
}

/// <summary>Dashboard và báo cáo doanh thu (mỗi lớp test có DB riêng).</summary>
public sealed class Wave5DashboardApiTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5DashboardApiTests(ApiFixture fx) : base(fx) { }

    private async Task<JsonElement> Summary(UserRole role)
    {
        var r = await (await Fx.As(role)).GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("no-store", r.Headers.CacheControl?.ToString());
        return await Json(r);
    }

    private static int[] Ids(JsonElement j, string k) => j.GetProperty(k).EnumerateArray().Select(x => x.GetProperty("BookingId").GetInt32()).ToArray();

    [LocalDbFact]
    public async Task Overdue_And_DueSoon_DayVsHourBookings()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var cust = (await Fx.SeedUser(UserRole.Customer)).Id;
        async Task<int> Rent(DateTime s, DateTime e, RentalMode m) => await Booking(await Car(CarStatus.DangThue), s, e, BookingStatus.DangThue, m, cust);

        var dayYesterday = await Rent(today.AddDays(-3), today.AddDays(-1), RentalMode.TheoNgay);   // hạn hôm nay 00:00 -> quá hạn
        var dayToday = await Rent(today.AddDays(-1), today, RentalMode.TheoNgay);                    // hạn hết hôm nay -> sắp trả (<24h)
        var dayTomorrow = await Rent(today, today.AddDays(1), RentalMode.TheoNgay);                  // hạn 00:00 ngày kia
        var hourOver = await Rent(now.AddHours(-8), now.AddHours(-1), RentalMode.TheoGio);
        var hourSoon = await Rent(now.AddHours(-2), now.AddHours(3), RentalMode.TheoGio);
        var hourLater = await Rent(now.AddHours(-2), now.AddHours(30), RentalMode.TheoGio);
        var hourAtMidnight = await Rent(today.AddHours(-6), today, RentalMode.TheoGio);              // đơn giờ kết thúc đúng 00:00 UTC -> đã quá hạn, không được coi như đơn ngày
        var notRenting = await Booking(await Car(), today.AddDays(-5), today.AddDays(-4), BookingStatus.DaXacNhan, RentalMode.TheoNgay, cust);

        var j = await Summary(UserRole.Staff);
        var over = Ids(j, "Overdue");
        var soon = Ids(j, "DueTodayOrTomorrow");
        Assert.Contains(dayYesterday, over);
        Assert.Contains(hourOver, over);
        Assert.Contains(hourAtMidnight, over);
        Assert.DoesNotContain(dayToday, over);
        Assert.Contains(dayToday, soon);
        Assert.Contains(hourSoon, soon);
        Assert.DoesNotContain(hourLater, soon);
        Assert.DoesNotContain(hourLater, over);
        Assert.DoesNotContain(dayTomorrow, over);
        Assert.DoesNotContain(notRenting, over.Concat(soon));
        Assert.True(j.GetProperty("RentingCount").GetInt32() >= 7);
    }

    [LocalDbFact]
    public async Task Counts_PendingConfirm_Documents_MaintenanceDue_CarsByStatus()
    {
        var before = await Summary(UserRole.Staff);
        var car = await Car();
        await Booking(car, DateTime.UtcNow.Date.AddDays(20), DateTime.UtcNow.Date.AddDays(21), BookingStatus.ChoXacNhan);
        await Plan(car, dueInDays: 3);
        await Plan(car, dueInDays: 30);          // ngoài 7 ngày
        await Plan(car, dueInDays: -2);          // quá hạn vẫn tính
        await Plan(car, st: MaintenanceStatus.Huy, dueInDays: 1); // đã hủy: không tính
        var after = await Summary(UserRole.Staff);
        Assert.Equal(before.GetProperty("PendingConfirm").GetInt32() + 1, after.GetProperty("PendingConfirm").GetInt32());
        Assert.Equal(before.GetProperty("MaintenanceDue").GetInt32() + 2, after.GetProperty("MaintenanceDue").GetInt32());
        int Cnt(JsonElement s) => s.GetProperty("CarsByStatus").EnumerateArray().Sum(x => x.GetProperty("Count").GetInt32());
        Assert.Equal(Cnt(before) + 1, Cnt(after));
    }

    [LocalDbFact]
    public async Task TodayRevenue_AdminOnly_ExcludesDepositTopUps_Unpaid_AndOtherDays()
    {
        var car = await Car();
        var bk = await Booking(car, DateTime.UtcNow.Date.AddDays(50), DateTime.UtcNow.Date.AddDays(51));
        var before = (await Summary(UserRole.Admin)).GetProperty("TodayRevenue").GetDecimal();
        await using (var db = Fx.NewContext())
        {
            var now = DateTime.UtcNow;
            db.Payments.Add(new Payment { BookingId = bk, Amount = 500_000, Method = PaymentMethod.TienMat, Status = PaymentStatus.DaThanhToan, PaidAt = now });
            db.Payments.Add(new Payment { BookingId = bk, Amount = 100_000, Method = PaymentMethod.ChuyenKhoan, Status = PaymentStatus.DaCoc, PaidAt = now });
            db.Payments.Add(new Payment { BookingId = bk, Amount = 999, Method = PaymentMethod.TienMat, Status = PaymentStatus.ChuaThanhToan, PaidAt = now });
            db.Payments.Add(new Payment { BookingId = bk, Amount = 888, Method = PaymentMethod.TienMat, Status = PaymentStatus.DaThanhToan, PaidAt = now.Date.AddDays(-1).AddHours(23) });
            db.Payments.Add(new Payment { BookingId = bk, Amount = 777, Method = PaymentMethod.TienMat, Status = PaymentStatus.DaHoanTien, PaidAt = now });
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 3_000_000, Method = PaymentMethod.TienMat });
            await db.SaveChangesAsync();
        }
        var admin = await Summary(UserRole.Admin);
        Assert.Equal(before + 600_000, admin.GetProperty("TodayRevenue").GetDecimal());
        var staff = await Summary(UserRole.Staff);
        Assert.Equal(JsonValueKind.Null, staff.GetProperty("TodayRevenue").ValueKind);
    }

    [LocalDbFact]
    public async Task RevenueReport_ExcludesDepositTopUps_AndHasByMethod()
    {
        var car = await Car();
        var bk = await Booking(car, DateTime.UtcNow.Date.AddDays(60), DateTime.UtcNow.Date.AddDays(61));
        var paid = new DateTime(2020, 3, 15, 10, 0, 0, DateTimeKind.Utc); // tháng không có dữ liệu khác
        await using (var db = Fx.NewContext())
        {
            db.Payments.Add(new Payment { BookingId = bk, Amount = 400_000, Method = PaymentMethod.TienMat, Status = PaymentStatus.DaThanhToan, PaidAt = paid });
            db.Payments.Add(new Payment { BookingId = bk, Amount = 200_000, Method = PaymentMethod.ChuyenKhoan, Status = PaymentStatus.DaThanhToan, PaidAt = paid });
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 5_000_000, Method = PaymentMethod.TienMat });
            await db.SaveChangesAsync();
        }
        var admin = await Fx.As(UserRole.Admin);
        var j = await Json(await admin.GetAsync("/api/reports/revenue?from=2020-03-01&to=2020-03-31"));
        Assert.Equal(600_000, j.GetProperty("TotalRevenue").GetDecimal());
        var methods = j.GetProperty("ByMethod").EnumerateArray().ToDictionary(x => x.GetProperty("Method").ToString(), x => x.GetProperty("Revenue").GetDecimal());
        Assert.Equal(2, methods.Count);
        Assert.Equal(600_000, methods.Values.Sum());

        // xuất xlsx khớp
        var x = await admin.GetAsync("/api/reports/revenue/export?from=2020-03-01&to=2020-03-31");
        Assert.Equal(HttpStatusCode.OK, x.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", x.Content.Headers.ContentType?.MediaType);
        using var wb = new XLWorkbook(await x.Content.ReadAsStreamAsync());
        Assert.Equal(600_000, wb.Worksheet("Theo tháng").Cell(2, 2).GetDouble());

        var pdf = await admin.GetAsync("/api/reports/revenue/export?format=pdf&from=2020-03-01&to=2020-03-31");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/reports/revenue/export?format=csv")).StatusCode);
        Assert.Equal("no-store", x.Headers.CacheControl?.ToString());
    }
}

public sealed class Wave5ImportApiTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5ImportApiTests(ApiFixture fx) : base(fx) { }

    private static byte[] Xlsx(params string[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Xe");
        string[] h = ["CarType", "Brand", "Model", "LicensePlate", "Seats", "CarAgeYears", "BasePricePerDay", "SecurityDeposit"];
        for (var c = 0; c < h.Length; c++) ws.Cell(1, c + 1).SetValue(h[c]);
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++) ws.Cell(r + 2, c + 1).SetValue(rows[r][c]);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string[] Row(string plate, string deposit = "") => ["Sedan", "Toyota", "Vios", plate, "5", "1", "800000", deposit];

    private static Task<HttpResponseMessage> Post(HttpClient c, byte[] file, bool dry, string name = "x.xlsx")
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(file), "file", name } };
        return c.PostAsync("/api/cars/import?dryRun=" + dry.ToString().ToLowerInvariant(), form);
    }

    private async Task<Car?> Find(string plate)
    {
        await using var db = Fx.NewContext();
        return await db.Cars.AsNoTracking().SingleOrDefaultAsync(c => c.LicensePlate == plate);
    }

    [LocalDbFact]
    public async Task Admin_Commit_CreatesSanSang_StaffCommit_CreatesChoDuyet()
    {
        var a = Uniq("IA-"); var s = Uniq("IS-");
        var ra = await Post(await Fx.As(UserRole.Admin), Xlsx(Row(a, "2000000")), false);
        Assert.Equal(HttpStatusCode.OK, ra.StatusCode);
        Assert.True((await Json(ra)).GetProperty("Committed").GetBoolean());
        var ca = await Find(a);
        Assert.Equal(CarStatus.SanSang, ca!.Status);
        Assert.Equal(2_000_000, ca.SecurityDeposit);

        var rs = await Post(await Fx.As(UserRole.Staff), Xlsx(Row(s)), false);
        Assert.True((await Json(rs)).GetProperty("Committed").GetBoolean());
        Assert.Equal(CarStatus.ChoDuyet, (await Find(s))!.Status);
    }

    [LocalDbFact]
    public async Task Staff_WithDeposit_IsRowError_AndNothingWritten()
    {
        var p1 = Uniq("IX-"); var p2 = Uniq("IY-");
        var r = await Post(await Fx.As(UserRole.Staff), Xlsx(Row(p1), Row(p2, "500000")), false);
        var j = await Json(r);
        Assert.False(j.GetProperty("Committed").GetBoolean());
        Assert.Contains(j.GetProperty("Errors").EnumerateArray(), e => e.GetProperty("Row").GetInt32() == 3 && e.GetProperty("Column").GetString() == "SecurityDeposit");
        Assert.Null(await Find(p1));
        Assert.Null(await Find(p2));
    }

    [LocalDbFact]
    public async Task DryRun_WritesNothing_DuplicateExistingPlate_IsRowError()
    {
        var p = Uniq("ID-");
        var admin = await Fx.As(UserRole.Admin);
        var dry = await Json(await Post(admin, Xlsx(Row(p)), true));
        Assert.False(dry.GetProperty("Committed").GetBoolean());
        Assert.Equal(1, dry.GetProperty("Valid").GetInt32());
        Assert.Null(await Find(p));

        await Post(admin, Xlsx(Row(p)), false);
        var again = await Json(await Post(admin, Xlsx(Row(p)), false));
        Assert.False(again.GetProperty("Committed").GetBoolean());
        Assert.Contains(again.GetProperty("Errors").EnumerateArray(), e => e.GetProperty("Column").GetString() == "LicensePlate");
    }

    [LocalDbFact]
    public async Task BadFiles_Are400_WithCodes()
    {
        var admin = await Fx.As(UserRole.Admin);
        var r1 = await Post(admin, [1, 2, 3], true, "x.csv");
        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Equal("IMPORT_INVALID_FILE", await Code(r1));
        var r2 = await Post(admin, [1, 2, 3], true);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        var r3 = await Post(admin, new byte[2 * 1024 * 1024 + 1], true);
        Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
        Assert.Equal("IMPORT_FILE_TOO_LARGE", await Code(r3));
    }

    [LocalDbFact]
    public async Task Template_IsOpenableXlsx()
    {
        var r = await (await Fx.As(UserRole.Staff)).GetAsync("/api/cars/import/template");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var wb = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        Assert.Equal("CarType", wb.Worksheets.First().Cell(1, 1).GetString());
    }

    /// <summary>Rủi ro coder nêu: hai request cùng biển số đồng thời không được trả 500 (phải là lỗi dòng hoặc kết quả hợp lệ).</summary>
    [LocalDbFact]
    public async Task ConcurrentImport_SamePlate_NeverReturns500()
    {
        var p = Uniq("IC-");
        var file = Xlsx(Row(p));
        var admin = await Fx.As(UserRole.Admin);
        var rs = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Post(admin, file, false)));
        Assert.DoesNotContain(rs, r => (int)r.StatusCode >= 500);
        await using var db = Fx.NewContext();
        Assert.Equal(1, await db.Cars.CountAsync(c => c.LicensePlate == p));
    }
}

public sealed class Wave5ExportApiTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5ExportApiTests(ApiFixture fx) : base(fx) { }

    [LocalDbFact]
    public async Task BookingsExport_StaffCanDownload_NeutralisesInjectedCustomerName()
    {
        var evil = await Fx.SeedUser(UserRole.Customer);
        await using (var db = Fx.NewContext())
        {
            var c = await db.Customers.SingleAsync(x => x.Id == evil.Id);
            c.FullName = "=HYPERLINK(\"http://evil\")";
            await db.SaveChangesAsync();
        }
        var car = await Car();
        await Booking(car, DateTime.UtcNow.Date.AddDays(80), DateTime.UtcNow.Date.AddDays(81), customerId: evil.Id);
        var r = await (await Fx.As(UserRole.Staff)).GetAsync($"/api/bookings/export?customerId={evil.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var wb = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        var cell = wb.Worksheets.First().Cell(2, 2);
        Assert.False(cell.HasFormula);
        Assert.True(cell.Style.IncludeQuotePrefix);
    }

    [LocalDbFact]
    public async Task Migration_AddCarMaintenance_AppliedAndNoPendingModelChanges()
    {
        await using var db = Fx.NewContext();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261117161900_AddCarMaintenance", applied);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}

public sealed class Wave5MaintenanceConcurrencyTests : Wave5Base, IClassFixture<ApiFixture>
{
    public Wave5MaintenanceConcurrencyTests(ApiFixture fx) : base(fx) { }

    [LocalDbFact]
    public async Task Complete_Concurrent_CreatesSingleFollowUpPlan()
    {
        var car = await Car();
        var id = await Plan(car, st: MaintenanceStatus.DangThucHien);
        await SetStatus(car, CarStatus.BaoTri);
        var admin = await Fx.As(UserRole.Admin);
        var body = new { NextDueDate = DateTime.UtcNow.Date.AddDays(90) };
        var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => admin.PostAsJsonAsync($"/api/maintenance/{id}/complete", body)));
        Assert.Equal(1, rs.Count(r => r.StatusCode == HttpStatusCode.OK));
        await using var db = Fx.NewContext();
        Assert.Equal(1, await db.Set<CarMaintenance>().CountAsync(m => m.CarId == car && m.Status == MaintenanceStatus.KeHoach));
    }
}
