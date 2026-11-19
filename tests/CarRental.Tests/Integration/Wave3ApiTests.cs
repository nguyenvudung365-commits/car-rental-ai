using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

/// <summary>Đợt 3 qua HTTP thật (LocalDB): biên bản giao/nhận, cọc, gia hạn, guard giao/trả xe, quyền, migration.</summary>
public sealed class Wave3ApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fx;
    public Wave3ApiTests(ApiFixture fx) => _fx = fx;

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();
    private static async Task<string?> Code(HttpResponseMessage r)
    {
        var j = await Json(r);
        return j.TryGetProperty("code", out var c) ? c.GetString() : null;
    }
    private static string Str(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString()! : e.GetRawText();

    private async Task<int> SeedCarDeposit(decimal deposit, decimal price = 700_000)
    {
        await using var db = _fx.NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..8],
            Seats = 5, CarAgeYears = 1, BasePricePerDay = price, Status = CarStatus.SanSang, SecurityDeposit = deposit
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    private async Task<int> SeedBooking(int customerId, int carId, BookingStatus status, int startOffset, int endOffset, decimal total = 2_100_000, decimal perDay = 700_000)
    {
        await using var db = _fx.NewContext();
        var b = new Booking
        {
            CustomerId = customerId, CarId = carId, Status = status, TotalPrice = total, FinalPricePerDay = perDay,
            StartDate = DateTime.UtcNow.Date.AddDays(startOffset), EndDate = DateTime.UtcNow.Date.AddDays(endOffset)
        };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    private async Task SeedApprovedDocs(int customerId)
    {
        await using var db = _fx.NewContext();
        db.Set<CustomerDocument>().Add(new CustomerDocument { CustomerId = customerId, Type = DocumentType.Cccd, Status = DocumentStatus.DaDuyet, Number = "012345678901", FrontKey = "private/x.webp" });
        db.Set<CustomerDocument>().Add(new CustomerDocument
        {
            CustomerId = customerId, Type = DocumentType.BangLai, Status = DocumentStatus.DaDuyet, Number = "987654321098", FrontKey = "private/y.webp",
            ExpiryDate = DateTime.UtcNow.Date.AddYears(2)
        });
        await db.SaveChangesAsync();
    }

    private static Task<HttpResponseMessage> PostHandover(HttpClient c, int bk, int type, int odo = 1000, int fuel = 80)
        => c.PostAsJsonAsync($"/api/bookings/{bk}/handovers", new { Type = type, Odometer = odo, FuelLevel = fuel, ConditionNote = "ok" });

    private static MultipartFormDataContent Webp(string name = "a.webp", string ct = "image/webp", byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes ?? WebpBytes.Tiny);
        file.Headers.ContentType = new MediaTypeHeaderValue(ct);
        form.Add(file, "file", name);
        return form;
    }

    // ---------- Biên bản ----------

    [LocalDbFact]
    public async Task Handover_Giao_Created_ThenSecondGiao409_HandoverExists()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);

        var r = await PostHandover(staff, bk, 1);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var again = await PostHandover(staff, bk, 1, odo: 2000);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("HANDOVER_EXISTS", await Code(again));

        await using var db = _fx.NewContext();
        Assert.Equal(1, await db.Set<HandoverRecord>().CountAsync(h => h.BookingId == bk));
    }

    [LocalDbFact]
    public async Task Handover_Nhan_Rules_StatusGiaoRequiredOdometerAndOnce()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var staff = await _fx.As(UserRole.Staff);

        var confirmed = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var wrongStatus = await PostHandover(staff, confirmed, 2);
        Assert.Equal(HttpStatusCode.Conflict, wrongStatus.StatusCode);
        Assert.Equal("INVALID_STATUS", await Code(wrongStatus));

        var renting = await SeedBooking(u.Id, await _fx.SeedCar(CarStatus.DangThue), BookingStatus.DangThue, -3, 0);
        var noGiao = await PostHandover(staff, renting, 2);
        Assert.Equal(HttpStatusCode.Conflict, noGiao.StatusCode);
        Assert.Equal("HANDOVER_REQUIRED", await Code(noGiao));

        await using (var db = _fx.NewContext())
        {
            db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = renting, Type = HandoverType.Giao, Odometer = 5000, FuelLevel = 80 });
            await db.SaveChangesAsync();
        }
        var lower = await PostHandover(staff, renting, 2, odo: 4999);
        Assert.Equal(HttpStatusCode.BadRequest, lower.StatusCode);
        Assert.Equal("ODOMETER_DECREASED", await Code(lower));

        Assert.Equal(HttpStatusCode.Created, (await PostHandover(staff, renting, 2, odo: 5000)).StatusCode); // biên: bằng số km giao
        var dup = await PostHandover(staff, renting, 2, odo: 5100);
        Assert.Equal("HANDOVER_EXISTS", await Code(dup));
    }

    [LocalDbFact]
    public async Task Handover_InvalidInputs_Return400_AndUnknownBooking404()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostHandover(staff, bk, 1, fuel: 101)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostHandover(staff, bk, 1, fuel: -1)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostHandover(staff, bk, 1, odo: -1)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostHandover(staff, bk, 9)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostHandover(staff, 99999999, 1)).StatusCode);
    }

    [LocalDbFact]
    public async Task HandoverPhoto_PrivatePrefix_NoKeyLeak_OwnerCanRead_OthersForbidden_NonWebpRejected()
    {
        var owner = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(owner.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);
        var hid = (await Json(await PostHandover(staff, bk, 1))).GetProperty("Id").GetInt32();

        var up = await staff.PostAsync($"/api/handovers/{hid}/photos", Webp());
        Assert.Equal(HttpStatusCode.Created, up.StatusCode);
        var pid = (await Json(up)).GetProperty("Id").GetInt32();

        var bad = await staff.PostAsync($"/api/handovers/{hid}/photos", Webp("a.jpg", "image/jpeg", new byte[] { 1, 2, 3 }));
        Assert.True((int)bad.StatusCode is >= 400 and < 500);

        await using (var db = _fx.NewContext())
            Assert.StartsWith("private/", (await db.Set<HandoverPhoto>().SingleAsync(p => p.Id == pid)).ObjectKey);

        var ownerClient = await _fx.LoginAs(owner.Email);
        var list = await ownerClient.GetAsync($"/api/bookings/{bk}/handovers");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.DoesNotContain("private/", await list.Content.ReadAsStringAsync());

        var photo = await ownerClient.GetAsync($"/api/handover-photos/{pid}");
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.Contains("no-store", string.Join(",", photo.Headers.GetValues("Cache-Control")));
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/api/handover-photos/{pid}")).StatusCode);

        var other = await _fx.As(UserRole.Customer);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/handover-photos/{pid}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/bookings/{bk}/handovers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().GetAsync($"/api/handover-photos/{pid}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync("/api/handover-photos/99999999")).StatusCode);
    }

    [LocalDbFact]
    public async Task HandoverPhoto_Limit10_Then11thConflict()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);
        var hid = (await Json(await PostHandover(staff, bk, 1))).GetProperty("Id").GetInt32();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Created, (await staff.PostAsync($"/api/handovers/{hid}/photos", Webp())).StatusCode);
        var r = await staff.PostAsync($"/api/handovers/{hid}/photos", Webp());
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("HANDOVER_PHOTO_LIMIT", await Code(r));
    }

    // ---------- Guard giao/trả xe ----------

    [LocalDbFact]
    public async Task Pickup_RequiresGiaoHandover_ThenOk_AndSurvivesNothingElse()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        await SeedApprovedDocs(u.Id);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);

        var blocked = await staff.PostAsync($"/api/bookings/{bk}/pickup", null);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("HANDOVER_REQUIRED", await Code(blocked));

        await PostHandover(staff, bk, 1);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/bookings/{bk}/pickup", null)).StatusCode);
    }

    [LocalDbFact]
    public async Task Pickup_WithDeposit_Required_UntilCollected()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        await SeedApprovedDocs(u.Id);
        var bk = await SeedBooking(u.Id, await SeedCarDeposit(2_000_000), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);
        await PostHandover(staff, bk, 1);

        var blocked = await staff.PostAsync($"/api/bookings/{bk}/pickup", null);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("DEPOSIT_REQUIRED", await Code(blocked));

        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/collect", new { Method = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/bookings/{bk}/pickup", null)).StatusCode);
    }

    [LocalDbFact]
    public async Task Return_RequiresNhanHandover_AndNoStateChangeWhenBlocked()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(CarStatus.DangThue), BookingStatus.DangThue, -3, 0);
        var staff = await _fx.As(UserRole.Staff);

        var r = await staff.PostAsJsonAsync($"/api/bookings/{bk}/return", new { ReturnedAt = DateTime.UtcNow.Date, DamageFee = 0 });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("HANDOVER_REQUIRED", await Code(r));
        await using var db = _fx.NewContext();
        Assert.Equal(BookingStatus.DangThue, (await db.Bookings.SingleAsync(b => b.Id == bk)).Status);
        Assert.False(await db.Set<ReturnRecord>().AnyAsync(x => x.BookingId == bk));
    }

    [LocalDbFact]
    public async Task CreatePayment_TruCoc_Rejected400()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var staff = await _fx.As(UserRole.Staff);
        var r = await staff.PostAsJsonAsync($"/api/bookings/{bk}/payments", new { Amount = 100_000, Method = 6 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("INVALID_METHOD", await Code(r));
    }

    // ---------- Cọc ----------

    [LocalDbFact]
    public async Task Deposit_Collect_Rules_NotRequired_Twice_TruCocMethod_WrongStatus()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var staff = await _fx.As(UserRole.Staff);

        var noDep = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var r0 = await staff.PostAsJsonAsync($"/api/bookings/{noDep}/deposit/collect", new { Method = 1 });
        Assert.Equal("DEPOSIT_NOT_REQUIRED", await Code(r0));

        var bk = await SeedBooking(u.Id, await SeedCarDeposit(2_000_000), BookingStatus.DaXacNhan, 0, 3);
        var tru = await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/collect", new { Method = 6 });
        Assert.Equal(HttpStatusCode.BadRequest, tru.StatusCode);
        Assert.Equal("INVALID_METHOD", await Code(tru));

        var ok = await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/collect", new { Method = 2 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var j = await Json(ok);
        Assert.Equal(2_000_000m, j.GetProperty("Collected").GetDecimal());
        Assert.Equal(2_000_000m, j.GetProperty("Held").GetDecimal());
        Assert.Equal("DangGiu", j.GetProperty("Status").GetString());

        var twice = await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/collect", new { Method = 1 });
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("DEPOSIT_COLLECTED", await Code(twice));

        var renting = await SeedBooking(u.Id, await SeedCarDeposit(1_000_000), BookingStatus.DangThue, -1, 2);
        Assert.Equal("INVALID_STATUS", await Code(await staff.PostAsJsonAsync($"/api/bookings/{renting}/deposit/collect", new { Method = 1 })));
    }

    [LocalDbFact]
    public async Task Deposit_Settle_DeductOutstandingThenRefundRest_PaymentTruCocCreated_Once()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCarDeposit(2_000_000), BookingStatus.DaTraXe, -4, -1, total: 1_500_000);
        await using (var db = _fx.NewContext())
        {
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 2_000_000, Method = PaymentMethod.TienMat });
            db.Set<Payment>().Add(new Payment { BookingId = bk, Amount = 1_000_000, Method = PaymentMethod.TienMat, Status = PaymentStatus.DaThanhToan, PaidAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var staff = await _fx.As(UserRole.Staff);
        var r = await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal(500_000m, j.GetProperty("Deducted").GetDecimal());
        Assert.Equal(1_500_000m, j.GetProperty("Refunded").GetDecimal());
        Assert.Equal(0m, j.GetProperty("Held").GetDecimal());
        Assert.Equal("DaQuyetToan", j.GetProperty("Status").GetString());

        await using (var db = _fx.NewContext())
        {
            var truCoc = await db.Set<Payment>().Where(p => p.BookingId == bk && p.Method == PaymentMethod.TruCoc).ToListAsync();
            Assert.Single(truCoc);
            Assert.Equal(500_000m, truCoc[0].Amount);
        }
        var again = await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("DEPOSIT_SETTLED", await Code(again));
    }

    [LocalDbFact]
    public async Task Deposit_Settle_DepositLessThanOutstanding_DeductsAllNoRefund()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCarDeposit(1_000_000), BookingStatus.DaTraXe, -4, -1, total: 2_100_000);
        await using (var db = _fx.NewContext())
        {
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 1_000_000, Method = PaymentMethod.TienMat });
            await db.SaveChangesAsync();
        }
        var j = await Json(await (await _fx.As(UserRole.Staff)).PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 }));
        Assert.Equal(1_000_000m, j.GetProperty("Deducted").GetDecimal());
        Assert.Equal(0m, j.GetProperty("Refunded").GetDecimal());
    }

    [LocalDbFact]
    public async Task Deposit_Settle_CancelledBooking_RefundsAllNoDeduction()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCarDeposit(2_000_000), BookingStatus.DaHuy, 0, 3);
        await using (var db = _fx.NewContext())
        {
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 2_000_000, Method = PaymentMethod.TienMat });
            await db.SaveChangesAsync();
        }
        var staff = await _fx.As(UserRole.Staff);
        var j = await Json(await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 2 }));
        Assert.Equal(0m, j.GetProperty("Deducted").GetDecimal());
        Assert.Equal(2_000_000m, j.GetProperty("Refunded").GetDecimal());
        await using var db2 = _fx.NewContext();
        Assert.False(await db2.Set<Payment>().AnyAsync(p => p.BookingId == bk && p.Method == PaymentMethod.TruCoc));
    }

    [LocalDbFact]
    public async Task Deposit_Settle_Guards_WrongStatus_NotCollected_TruCocRefund()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var staff = await _fx.As(UserRole.Staff);
        var renting = await SeedBooking(u.Id, await SeedCarDeposit(1_000_000), BookingStatus.DangThue, -1, 2);
        Assert.Equal("INVALID_STATUS", await Code(await staff.PostAsJsonAsync($"/api/bookings/{renting}/deposit/settle", new { RefundMethod = 1 })));

        var done = await SeedBooking(u.Id, await SeedCarDeposit(1_000_000), BookingStatus.DaTraXe, -4, -1);
        Assert.Equal("DEPOSIT_NOT_COLLECTED", await Code(await staff.PostAsJsonAsync($"/api/bookings/{done}/deposit/settle", new { RefundMethod = 1 })));
        Assert.Equal("INVALID_METHOD", await Code(await staff.PostAsJsonAsync($"/api/bookings/{done}/deposit/settle", new { RefundMethod = 6 })));
    }

    [LocalDbFact]
    public async Task Deposit_Get_OwnerOk_OtherCustomerForbidden_ChuaThuShowsRequired()
    {
        var owner = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(owner.Id, await SeedCarDeposit(3_000_000), BookingStatus.DaXacNhan, 0, 3);
        var j = await Json(await (await _fx.LoginAs(owner.Email)).GetAsync($"/api/bookings/{bk}/deposit"));
        Assert.Equal("ChuaThu", j.GetProperty("Status").GetString());
        Assert.Equal(3_000_000m, j.GetProperty("Required").GetDecimal());
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _fx.As(UserRole.Customer)).GetAsync($"/api/bookings/{bk}/deposit")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await (await _fx.As(UserRole.Staff)).GetAsync("/api/bookings/99999999/deposit")).StatusCode);
    }

    // ---------- Gia hạn ----------

    [LocalDbFact]
    public async Task Extension_Request_Approve_UpdatesEndDateAndTotal_ChargeUsesLockedPrice()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3, total: 2_100_000, perDay: 700_000);
        var cust = await _fx.LoginAs(u.Email);
        var newEnd = DateTime.UtcNow.Date.AddDays(5);

        var check = await Json(await cust.GetAsync($"/api/bookings/{bk}/extend/check?newEndDate={newEnd:yyyy-MM-dd}"));
        Assert.True(check.GetProperty("Available").GetBoolean());
        Assert.Equal(2, check.GetProperty("ExtraDays").GetInt32());
        Assert.Equal(1_400_000m, check.GetProperty("ExtraAmount").GetDecimal());
        Assert.Equal(3_500_000m, check.GetProperty("NewTotalPrice").GetDecimal());

        var req = await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = newEnd });
        Assert.Equal(HttpStatusCode.Created, req.StatusCode);
        var extId = (await Json(req)).GetProperty("Id").GetInt32();
        await using (var db = _fx.NewContext())
            Assert.Equal(DateTime.UtcNow.Date.AddDays(3), (await db.Bookings.SingleAsync(b => b.Id == bk)).EndDate); // chưa đổi trước khi duyệt

        var dup = await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = newEnd.AddDays(1) });
        Assert.Equal("EXTENSION_PENDING", await Code(dup));

        var staff = await _fx.As(UserRole.Staff);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/booking-extensions/{extId}/approve", null)).StatusCode);
        await using (var db = _fx.NewContext())
        {
            var b = await db.Bookings.SingleAsync(x => x.Id == bk);
            Assert.Equal(newEnd, b.EndDate);
            Assert.Equal(3_500_000m, b.TotalPrice);
        }
        var again = await staff.PostAsync($"/api/booking-extensions/{extId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        await using var db2 = _fx.NewContext();
        Assert.Equal(3_500_000m, (await db2.Bookings.SingleAsync(x => x.Id == bk)).TotalPrice); // không cộng hai lần
    }

    [LocalDbFact]
    public async Task Extension_Reject_NeedsReason_LeavesBookingUnchanged_AllowsNewRequest()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var cust = await _fx.LoginAs(u.Email);
        var extId = (await Json(await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = DateTime.UtcNow.Date.AddDays(4) }))).GetProperty("Id").GetInt32();
        var staff = await _fx.As(UserRole.Staff);

        var noReason = await staff.PostAsJsonAsync($"/api/booking-extensions/{extId}/reject", new { Reason = " " });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal("REASON_REQUIRED", await Code(noReason));

        var rej = await staff.PostAsJsonAsync($"/api/booking-extensions/{extId}/reject", new { Reason = "Xe đã có hẹn bảo dưỡng" });
        Assert.Equal(HttpStatusCode.OK, rej.StatusCode);
        Assert.Equal("TuChoi", Str((await Json(rej)).GetProperty("Status")) is "3" ? "TuChoi" : Str((await Json(rej)).GetProperty("Status")));
        await using (var db = _fx.NewContext())
            Assert.Equal(DateTime.UtcNow.Date.AddDays(3), (await db.Bookings.SingleAsync(b => b.Id == bk)).EndDate);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync($"/api/booking-extensions/{extId}/reject", new { Reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = DateTime.UtcNow.Date.AddDays(5) })).StatusCode);
    }

    [LocalDbFact]
    public async Task Extension_Validation_NotLaterTooLongWrongStatus_AndOverlap()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var car = await _fx.SeedCar();
        var bk = await SeedBooking(u.Id, car, BookingStatus.DaXacNhan, 0, 3);
        var cust = await _fx.LoginAs(u.Email);
        var today = DateTime.UtcNow.Date;

        var same = await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = today.AddDays(3) });
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.Equal("INVALID_NEW_END_DATE", await Code(same));
        Assert.Equal(HttpStatusCode.BadRequest, (await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = today.AddDays(1) })).StatusCode);

        var tooLong = await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = today.AddDays(34) }); // +31
        Assert.Equal("EXTENSION_TOO_LONG", await Code(tooLong));
        // biên: +30 ngày hợp lệ (không bị chặn bởi giới hạn)
        var edge = await cust.GetAsync($"/api/bookings/{bk}/extend/check?newEndDate={today.AddDays(33):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, edge.StatusCode);

        await SeedBooking((await _fx.SeedUser(UserRole.Customer)).Id, car, BookingStatus.DaXacNhan, 4, 6); // xe có lịch kế tiếp
        var overlap = await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = today.AddDays(5) });
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal("EXTENSION_UNAVAILABLE", await Code(overlap));
        var check = await Json(await cust.GetAsync($"/api/bookings/{bk}/extend/check?newEndDate={today.AddDays(5):yyyy-MM-dd}"));
        Assert.False(check.GetProperty("Available").GetBoolean());

        var done = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaTraXe, -5, -2);
        Assert.Equal("INVALID_STATUS", await Code(await cust.PostAsJsonAsync($"/api/bookings/{done}/extend", new { NewEndDate = today.AddDays(1) })));
    }

    [LocalDbFact]
    public async Task Extension_Approve_RechecksOverlap_WhenAnotherBookingAppearedAfterRequest()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var car = await _fx.SeedCar();
        var bk = await SeedBooking(u.Id, car, BookingStatus.DaXacNhan, 0, 3);
        var cust = await _fx.LoginAs(u.Email);
        var extId = (await Json(await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = DateTime.UtcNow.Date.AddDays(5) }))).GetProperty("Id").GetInt32();
        await SeedBooking((await _fx.SeedUser(UserRole.Customer)).Id, car, BookingStatus.DaXacNhan, 4, 6);

        var r = await (await _fx.As(UserRole.Staff)).PostAsync($"/api/booking-extensions/{extId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("EXTENSION_UNAVAILABLE", await Code(r));
        await using var db = _fx.NewContext();
        Assert.Equal(DateTime.UtcNow.Date.AddDays(3), (await db.Bookings.SingleAsync(b => b.Id == bk)).EndDate);
    }

    [LocalDbFact]
    public async Task Extension_OtherCustomerForbidden_StaffCanListPending()
    {
        var owner = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(owner.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        var other = await _fx.As(UserRole.Customer);
        var body = new { NewEndDate = DateTime.UtcNow.Date.AddDays(4) };
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/api/bookings/{bk}/extend", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/bookings/{bk}/extensions")).StatusCode);

        await (await _fx.LoginAs(owner.Email)).PostAsJsonAsync($"/api/bookings/{bk}/extend", body);
        var pending = await Json(await (await _fx.As(UserRole.Staff)).GetAsync("/api/booking-extensions/pending"));
        Assert.Contains(pending.EnumerateArray(), e => e.GetProperty("BookingId").GetInt32() == bk);
    }

    [Theory]
    [InlineData(700_000, 2, 1_400_000)]
    [InlineData(700_000, 1, 700_000)]
    [InlineData(300_000, 2, 800_000)]     // kẹp sàn 400k
    [InlineData(2_000_000, 1, 1_500_000)] // kẹp trần 1.5tr
    [InlineData(700_000, 0, 0)]
    public void PricingCalculator_ExtensionCharge(decimal perDay, int days, decimal expected)
        => Assert.Equal(expected, PricingCalculator.ExtensionCharge(perDay, days));

    // ---------- Cọc xe chỉ Admin đặt ----------

    private static object CarBody(decimal? deposit) => new
    {
        CarType = 1, Brand = "Toyota", Model = "Camry", LicensePlate = "T-" + Guid.NewGuid().ToString("N")[..8], Seats = 5, CarAgeYears = 1,
        BasePricePerDay = 700_000, Description = "d", SecurityDeposit = deposit
    };

    [LocalDbFact]
    public async Task Car_SecurityDeposit_AdminOnly_OnCreateAndUpdate()
    {
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);

        var s = await staff.PostAsJsonAsync("/api/cars", CarBody(5_000_000));
        Assert.Equal(HttpStatusCode.Forbidden, s.StatusCode);
        Assert.Equal("PRICE_ADMIN_ONLY", await Code(s));
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync("/api/cars", CarBody(null))).StatusCode);   // Staff không đặt cọc thì tạo được
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync("/api/cars", CarBody(0))).StatusCode);

        var a = await admin.PostAsJsonAsync("/api/cars", CarBody(5_000_000));
        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        var created = await Json(a);
        Assert.Equal(5_000_000m, created.GetProperty("SecurityDeposit").GetDecimal());
        var id = created.GetProperty("Id").GetInt32();

        object Upd(decimal? d) => new
        {
            CarType = 1, Brand = "Toyota", Model = "Camry", LicensePlate = created.GetProperty("LicensePlate").GetString(), Seats = 5, CarAgeYears = 1,
            BasePricePerDay = 700_000, Status = 1, Description = "d", SecurityDeposit = d
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync($"/api/cars/{id}", Upd(1_000_000))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsJsonAsync($"/api/cars/{id}", Upd(null))).StatusCode);      // null = giữ nguyên
        Assert.Equal(HttpStatusCode.NoContent, (await staff.PutAsJsonAsync($"/api/cars/{id}", Upd(5_000_000))).StatusCode); // không đổi
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/cars/{id}", Upd(1_000_000))).StatusCode);
        await using var db = _fx.NewContext();
        Assert.Equal(1_000_000m, (await db.Cars.SingleAsync(c => c.Id == id)).SecurityDeposit);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/cars", CarBody(-1))).StatusCode);
    }

    // ---------- Ma trận quyền HTTP ----------

    [LocalDbFact]
    public async Task NewRoutes_Anonymous401_AndWrongRole403()
    {
        var anon = _fx.Anonymous();
        var cust = await _fx.As(UserRole.Customer);
        var json = JsonContent.Create(new { });
        (HttpMethod M, string Url)[] all =
        {
            (HttpMethod.Get, "/api/bookings/1/handovers"), (HttpMethod.Post, "/api/bookings/1/handovers"),
            (HttpMethod.Post, "/api/handovers/1/photos"), (HttpMethod.Get, "/api/handover-photos/1"),
            (HttpMethod.Get, "/api/bookings/1/deposit"), (HttpMethod.Post, "/api/bookings/1/deposit/collect"), (HttpMethod.Post, "/api/bookings/1/deposit/settle"),
            (HttpMethod.Get, "/api/bookings/1/extend/check?newEndDate=2030-01-01"), (HttpMethod.Post, "/api/bookings/1/extend"),
            (HttpMethod.Get, "/api/bookings/1/extensions"), (HttpMethod.Get, "/api/booking-extensions/pending"),
            (HttpMethod.Post, "/api/booking-extensions/1/approve"), (HttpMethod.Post, "/api/booking-extensions/1/reject")
        };
        foreach (var (m, url) in all)
        {
            var resp = await anon.SendAsync(new HttpRequestMessage(m, url) { Content = url.Contains("/photos") ? Webp() : m == HttpMethod.Post ? JsonContent.Create(new { }) : null });
            Assert.True(HttpStatusCode.Unauthorized == resp.StatusCode, $"401 {m} {url} -> {(int)resp.StatusCode}");
        }

        var staffOnly = new[]
        {
            (HttpMethod.Post, "/api/bookings/1/handovers"), (HttpMethod.Post, "/api/handovers/1/photos"),
            (HttpMethod.Post, "/api/bookings/1/deposit/collect"), (HttpMethod.Post, "/api/bookings/1/deposit/settle"),
            (HttpMethod.Get, "/api/booking-extensions/pending"), (HttpMethod.Post, "/api/booking-extensions/1/approve"), (HttpMethod.Post, "/api/booking-extensions/1/reject")
        };
        foreach (var (m, url) in staffOnly)
        {
            var resp = await cust.SendAsync(new HttpRequestMessage(m, url) { Content = url.Contains("/photos") ? Webp() : m == HttpMethod.Post ? JsonContent.Create(new { }) : null });
            Assert.True(HttpStatusCode.Forbidden == resp.StatusCode, $"403 {m} {url} -> {(int)resp.StatusCode}");
        }
    }

    // ---------- Phần 0 ----------

    [LocalDbFact]
    public async Task AdminCustomerDetail_StaffSeesLast4Only_AdminSeesFull_NonCustomer404()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        await SeedApprovedDocs(u.Id);
        var staffUser = await _fx.SeedUser(UserRole.Staff);

        var staffView = await (await _fx.As(UserRole.Staff)).GetAsync($"/api/admin/customers/{u.Id}");
        var sText = await staffView.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, staffView.StatusCode);
        Assert.DoesNotContain("012345678901", sText);
        Assert.DoesNotContain("987654321098", sText);
        Assert.Contains("********8901", sText);
        Assert.Contains("********1098", sText);

        var adminText = await (await (await _fx.As(UserRole.Admin)).GetAsync($"/api/admin/customers/{u.Id}")).Content.ReadAsStringAsync();
        Assert.Contains("012345678901", adminText);

        Assert.Equal(HttpStatusCode.NotFound, (await (await _fx.As(UserRole.Admin)).GetAsync($"/api/admin/customers/{staffUser.Id}")).StatusCode);
    }

    [Fact]
    public async Task LocalFileStorage_PrivatePrefix_ThrowsNotSupported()
    {
        var env = new StubEnv(Path.Combine(Path.GetTempPath(), "cr-" + Guid.NewGuid().ToString("N")));
        var svc = new CarRental.Infrastructure.Files.LocalFileStorageService(env);
        var file = new FormFile(new MemoryStream(WebpBytes.Tiny), 0, WebpBytes.Tiny.Length, "file", "a.webp") { Headers = new HeaderDictionary(), ContentType = "image/webp" };
        await Assert.ThrowsAsync<NotSupportedException>(() => svc.SaveAsync(file, "private/handovers/1"));
        await Assert.ThrowsAsync<NotSupportedException>(() => svc.SaveAsync(file, "/PRIVATE/x"));
    }

    private sealed class StubEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public StubEnv(string root) => WebRootPath = root;
        public string WebRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "t";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Testing";
    }

    // ---------- Migration ----------

    [LocalDbFact]
    public async Task Migration_AddHandoverDepositExtension_AppliedAndNoPendingModelChanges()
    {
        await using var db = _fx.NewContext();
        Assert.Contains("20261117145144_AddHandoverDepositExtension", await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [LocalDbFact]
    public async Task Db_Unique_HandoverPerBookingAndType_AndSinglePendingExtension()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await _fx.SeedCar(), BookingStatus.DaXacNhan, 0, 3);
        await using var db = _fx.NewContext();
        db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bk, Type = HandoverType.Giao, Odometer = 1, FuelLevel = 1 });
        await db.SaveChangesAsync();
        await using var db2 = _fx.NewContext();
        db2.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bk, Type = HandoverType.Giao, Odometer = 2, FuelLevel = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());

        await using var db3 = _fx.NewContext();
        for (var i = 0; i < 2; i++)
            db3.Set<BookingExtension>().Add(new BookingExtension { BookingId = bk, OldEndDate = DateTime.UtcNow.Date, NewEndDate = DateTime.UtcNow.Date.AddDays(1 + i), RequestedById = u.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => db3.SaveChangesAsync());
    }
}
