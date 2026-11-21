using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

/// <summary>Sửa theo review đợt 3: khoản chờ chặn quyết toán cọc, duyệt/từ chối gia hạn song song, doanh thu không tính cọc.</summary>
public sealed class Wave3ReviewTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fx;
    public Wave3ReviewTests(ApiFixture fx) => _fx = fx;

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();
    private static async Task<string?> Code(HttpResponseMessage r)
        => (await Json(r)).TryGetProperty("code", out var c) ? c.GetString() : null;

    private async Task<int> SeedCar(decimal deposit)
    {
        await using var db = _fx.NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51B-" + Guid.NewGuid().ToString("N")[..8],
            Seats = 5, CarAgeYears = 1, BasePricePerDay = 700_000, Status = CarStatus.SanSang, SecurityDeposit = deposit
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    private async Task<int> SeedBooking(int customerId, int carId, BookingStatus status, int startOffset, int endOffset, decimal total)
    {
        await using var db = _fx.NewContext();
        var b = new Booking
        {
            CustomerId = customerId, CarId = carId, Status = status, TotalPrice = total, FinalPricePerDay = 700_000,
            StartDate = DateTime.UtcNow.Date.AddDays(startOffset), EndDate = DateTime.UtcNow.Date.AddDays(endOffset)
        };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    [LocalDbFact]
    public async Task Settle_PendingPayment_409_ThenAllowedAfterMarkPaid()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCar(5_000_000), BookingStatus.DaTraXe, -4, -1, total: 4_200_000);
        int pendingId;
        await using (var db = _fx.NewContext())
        {
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 5_000_000, Method = PaymentMethod.TienMat });
            var p = new Payment { BookingId = bk, Amount = 3_000_000, Method = PaymentMethod.TienMat, Status = PaymentStatus.ChuaThanhToan };
            db.Set<Payment>().Add(p);
            await db.SaveChangesAsync();
            pendingId = p.Id;
        }
        var staff = await _fx.As(UserRole.Staff);
        var blocked = await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("PAYMENT_PENDING", await Code(blocked));
        await using (var db = _fx.NewContext())
            Assert.False(await db.Set<DepositTransaction>().AnyAsync(t => t.BookingId == bk && t.Type != DepositTxType.Thu));

        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync($"/api/payments/{pendingId}/mark-paid", new { Status = 3 })).StatusCode);
        var ok = await Json(await staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 }));
        Assert.Equal(1_200_000m, ok.GetProperty("Deducted").GetDecimal());
        Assert.Equal(3_800_000m, ok.GetProperty("Refunded").GetDecimal());
    }

    [LocalDbFact]
    public async Task Settle_Parallel_OnlyOneSucceeds_SingleDeduction()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCar(2_000_000), BookingStatus.DaTraXe, -4, -1, total: 1_500_000);
        await using (var db = _fx.NewContext())
        {
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 2_000_000, Method = PaymentMethod.TienMat });
            await db.SaveChangesAsync();
        }
        var staff = await _fx.As(UserRole.Staff);
        var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => staff.PostAsJsonAsync($"/api/bookings/{bk}/deposit/settle", new { RefundMethod = 1 })));
        Assert.Equal(1, rs.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(3, rs.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        await using var db2 = _fx.NewContext();
        Assert.Single(await db2.Set<Payment>().Where(p => p.BookingId == bk && p.Method == PaymentMethod.TruCoc).ToListAsync());
        Assert.Equal(1, await db2.Set<DepositTransaction>().CountAsync(t => t.BookingId == bk && t.Type == DepositTxType.Hoan));
    }

    [LocalDbFact]
    public async Task Extension_ApproveAndReject_Parallel_OnlyOneWins_StateConsistent()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCar(0), BookingStatus.DaXacNhan, 0, 3, total: 2_100_000);
        var cust = await _fx.LoginAs(u.Email);
        var extId = (await Json(await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = DateTime.UtcNow.Date.AddDays(5) }))).GetProperty("Id").GetInt32();
        var staff = await _fx.As(UserRole.Staff);

        var rs = await Task.WhenAll(new[]
        {
            staff.PostAsync($"/api/booking-extensions/{extId}/approve", null),
            staff.PostAsync($"/api/booking-extensions/{extId}/approve", null),
            staff.PostAsJsonAsync($"/api/booking-extensions/{extId}/reject", new { Reason = "ban" }),
            staff.PostAsJsonAsync($"/api/booking-extensions/{extId}/reject", new { Reason = "ban" })
        });
        Assert.Equal(1, rs.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(3, rs.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        await using var db = _fx.NewContext();
        var ext = await db.Set<BookingExtension>().SingleAsync(e => e.Id == extId);
        var b = await db.Bookings.SingleAsync(x => x.Id == bk);
        if (ext.Status == ExtensionStatus.DaDuyet)
        {
            Assert.Equal(DateTime.UtcNow.Date.AddDays(5), b.EndDate);
            Assert.Equal(2_100_000m + 1_400_000m, b.TotalPrice);
        }
        else
        {
            Assert.Equal(ExtensionStatus.TuChoi, ext.Status);
            Assert.Equal(DateTime.UtcNow.Date.AddDays(3), b.EndDate);
            Assert.Equal(2_100_000m, b.TotalPrice);
        }
    }

    [LocalDbFact]
    public async Task Extension_LessThan24Hours_Rejected_SameDaysFormulaAsBooking()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCar(0), BookingStatus.DaXacNhan, 0, 3, total: 2_100_000);
        var cust = await _fx.LoginAs(u.Email);
        var r = await cust.PostAsJsonAsync($"/api/bookings/{bk}/extend", new { NewEndDate = DateTime.UtcNow.Date.AddDays(3).AddHours(23) });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("INVALID_NEW_END_DATE", await Code(r));
    }

    [LocalDbFact]
    public async Task Revenue_Report_IgnoresDepositCollected()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var bk = await SeedBooking(u.Id, await SeedCar(5_000_000), BookingStatus.DaTraXe, -4, -1, total: 1_000_000);
        var paidAt = new DateTime(2031, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        await using (var db = _fx.NewContext())
        {
            db.Set<Payment>().Add(new Payment { BookingId = bk, Amount = 1_000_000, Method = PaymentMethod.TienMat, Status = PaymentStatus.DaThanhToan, PaidAt = paidAt });
            db.Set<DepositTransaction>().Add(new DepositTransaction { BookingId = bk, Type = DepositTxType.Thu, Amount = 5_000_000, Method = PaymentMethod.TienMat });
            await db.SaveChangesAsync();
        }
        var j = await Json(await (await _fx.As(UserRole.Admin)).GetAsync("/api/reports/revenue?from=2031-03-01&to=2031-03-31"));
        Assert.Equal(1_000_000m, j.GetProperty("TotalRevenue").GetDecimal());
    }
}
