using System.Text;
using CarRental.Application.Common;
using CarRental.Application.Modules.Bookings;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Pdf;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests.Integration;

/// <summary>
/// Luồng xác nhận -> thanh toán -> nhận xe -> trả xe -> hợp đồng trên SQL Server thật (DB tạm, tự xóa).
/// Test được ĐÁNH DẤU SKIPPED (không phải xanh) nếu không có SQL Server/LocalDB.
/// </summary>
public sealed class RentalLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly string _dbName = "CarRentalTest_" + Guid.NewGuid().ToString("N");
    private string _conn = "";
    private bool _ready;

    public async Task InitializeAsync()
    {
        _conn = LocalDbFactAttribute.ServerConnection + ";Database=" + _dbName;
        try
        {
            await using var db = NewContext();
            await db.Database.MigrateAsync();
            _ready = true;
        }
        catch (Exception)
        {
            _ready = false; // các test [LocalDbFact] đã bị Skip nếu server không có; nếu lỗi migrate thì test sẽ fail ở NewContext
        }
    }

    public async Task DisposeAsync()
    {
        if (!_ready) return;
        await using var db = NewContext();
        await db.Database.EnsureDeletedAsync();
    }

    private ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_conn).Options);

    private static BookingService NewBookingService(ApplicationDbContext db) =>
        new(new BookingRepository(db), new CarRepository(db), new FakeAi(), db);

    private static RentalService NewRental(ApplicationDbContext db) =>
        new(new BookingRepository(db), NewBookingService(db), db, new ContractPdfGenerator());

    private async Task<(int carId, int customerId, int otherCustomerId)> SeedAsync()
    {
        Assert.True(_ready, "DB tạm không khởi tạo được (migrate lỗi).");
        await using var db = NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..6],
            Seats = 5, CarAgeYears = 2, BasePricePerDay = 700_000, Status = CarStatus.SanSang
        };
        Customer NewCustomer(string name) => new()
        {
            FullName = name, Email = Guid.NewGuid().ToString("N") + "@t.vn", PhoneNumber = Guid.NewGuid().ToString("N")[..10],
            PasswordHash = "x", IdentityNumber = "079123456789", Address = "Đường Lê Lợi, Quận 1"
        };
        var c1 = NewCustomer("Nguyễn Thị Hương");
        var c2 = NewCustomer("Khách Khác");
        db.Cars.Add(car);
        db.Customers.AddRange(c1, c2);
        await db.SaveChangesAsync();
        return (car.Id, c1.Id, c2.Id);
    }

    private static CreateBookingRequest Req(int carId, int fromDays, int toDays) =>
        new(carId, DateTime.UtcNow.Date.AddDays(fromDays), DateTime.UtcNow.Date.AddDays(toDays), null);

    /// <summary>Tạo + xác nhận booking (DaXacNhan). Giá chốt/ngày = 900.000 (FakeAi), 2 ngày => 1.800.000.</summary>
    private async Task<int> ConfirmedBookingAsync(int carId, int customerId, int from = 1, int to = 3)
    {
        await using var db = NewContext();
        var dto = await NewBookingService(db).CreateAsync(customerId, Req(carId, from, to));
        await NewBookingService(db).ConfirmAsync(dto.Id);
        return dto.Id;
    }

    private async Task<CarStatus> CarStatusAsync(int carId)
    {
        await using var db = NewContext();
        return (await db.Cars.AsNoTracking().SingleAsync(c => c.Id == carId)).Status;
    }

    private async Task<BookingStatus> BookingStatusAsync(int id)
    {
        await using var db = NewContext();
        return (await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == id)).Status;
    }

    [LocalDbFact]
    public async Task FullFlow_Confirm_Pay_Pickup_Return_ChangesStatusesCorrectly()
    {
        var (carId, custId, _) = await SeedAsync();
        var bookingId = await ConfirmedBookingAsync(carId, custId);
        Assert.Equal(BookingStatus.DaXacNhan, await BookingStatusAsync(bookingId));
        Assert.Equal(CarStatus.SanSang, await CarStatusAsync(carId));

        PaymentDto pay;
        await using (var db = NewContext())
        {
            var rental = NewRental(db);
            pay = await rental.CreatePaymentAsync(bookingId, new CreatePaymentRequest(1_800_000, PaymentMethod.ChuyenKhoan, "TX1"), custId);
            Assert.Equal("ChuaThanhToan", pay.Status);
            var paid = await rental.MarkPaymentPaidAsync(pay.Id, new MarkPaymentPaidRequest(PaymentStatus.DaThanhToan, null));
            Assert.Equal("DaThanhToan", paid.Status);
            Assert.NotNull(paid.PaidAt);
        }

        await using (var db = NewContext())
        {
            var dto = await NewRental(db).PickupAsync(bookingId);
            Assert.Equal("DangThue", dto.Status);
        }
        Assert.Equal(BookingStatus.DangThue, await BookingStatusAsync(bookingId));
        Assert.Equal(CarStatus.DangThue, await CarStatusAsync(carId));

        await using (var db = NewContext())
        {
            var end = DateTime.UtcNow.Date.AddDays(3);
            var rr = await NewRental(db).ReturnAsync(bookingId, new ReturnBookingRequest(end, 0, null), staffId: null);
            Assert.Equal(0, rr.LateDays);
            Assert.Equal(1_800_000m, rr.FinalAmount);
        }
        Assert.Equal(BookingStatus.DaTraXe, await BookingStatusAsync(bookingId));
        Assert.Equal(CarStatus.SanSang, await CarStatusAsync(carId));
    }

    [LocalDbFact]
    public async Task Return_Late_ComputesLateFeeAndFinalAmount_AndPersistsRecord()
    {
        var (carId, custId, _) = await SeedAsync();
        var bookingId = await ConfirmedBookingAsync(carId, custId); // 2 ngày x 900.000
        await using (var db = NewContext()) await NewRental(db).PickupAsync(bookingId);

        var end = DateTime.UtcNow.Date.AddDays(3);
        await using (var db = NewContext())
        {
            var rr = await NewRental(db).ReturnAsync(bookingId, new ReturnBookingRequest(end.AddDays(2).AddHours(5), 300_000, "Xước cản"), 1);
            Assert.Equal(2, rr.LateDays);
            Assert.Equal(1_800_000m, rr.LateFee);               // 2 x 900.000
            Assert.Equal(300_000m, rr.DamageFee);
            Assert.Equal(1_800_000m + 1_800_000m + 300_000m, rr.FinalAmount);
        }

        await using var verify = NewContext();
        var saved = await verify.ReturnRecords.SingleAsync(r => r.BookingId == bookingId);
        Assert.Equal(2, saved.LateDays);
        Assert.Equal("Xước cản", saved.DamageNote);

        var payments = await NewRental(verify).GetPaymentsAsync(bookingId, custId);
        Assert.Equal(3_900_000m, payments.AmountToPay); // AmountToPay chuyển sang FinalAmount sau khi trả xe
    }

    [LocalDbFact]
    public async Task Return_Early_NoLateFee()
    {
        var (carId, custId, _) = await SeedAsync();
        var bookingId = await ConfirmedBookingAsync(carId, custId, 1, 5);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(bookingId);
        await using (var db = NewContext())
        {
            var rr = await NewRental(db).ReturnAsync(bookingId, new ReturnBookingRequest(DateTime.UtcNow.Date.AddDays(2), 0, null), null);
            Assert.Equal(0, rr.LateDays);
            Assert.Equal(0m, rr.LateFee);
        }
    }

    [LocalDbFact]
    public async Task Return_BeforeStartDate_Rejected_AndStateUnchanged()
    {
        var (carId, custId, _) = await SeedAsync();
        var bookingId = await ConfirmedBookingAsync(carId, custId, 2, 4);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(bookingId);
        await using (var db = NewContext())
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                NewRental(db).ReturnAsync(bookingId, new ReturnBookingRequest(DateTime.UtcNow.Date, 0, null), null));
        }
        Assert.Equal(BookingStatus.DangThue, await BookingStatusAsync(bookingId));
        Assert.Equal(CarStatus.DangThue, await CarStatusAsync(carId));
    }

    [LocalDbFact]
    public async Task Pickup_WhenNotConfirmed_Throws409_AndCarUnchanged()
    {
        var (carId, custId, _) = await SeedAsync();
        int pendingId;
        await using (var db = NewContext())
            pendingId = (await NewBookingService(db).CreateAsync(custId, Req(carId, 1, 3))).Id;

        await using (var db = NewContext())
        {
            var ex = await Assert.ThrowsAsync<ConflictException>(() => NewRental(db).PickupAsync(pendingId));
            Assert.Equal("INVALID_STATUS", ex.Code);
        }
        Assert.Equal(CarStatus.SanSang, await CarStatusAsync(carId));
        Assert.Equal(BookingStatus.ChoXacNhan, await BookingStatusAsync(pendingId));
    }

    [LocalDbFact]
    public async Task Pickup_Twice_SecondThrows409()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(id);
        await using (var db = NewContext())
            await Assert.ThrowsAsync<ConflictException>(() => NewRental(db).PickupAsync(id));
    }

    [LocalDbFact]
    public async Task Return_WhenNotRenting_Throws409_AndCarStatusUntouched()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId); // DaXacNhan, chưa nhận xe
        await using (var db = NewContext())
        {
            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                NewRental(db).ReturnAsync(id, new ReturnBookingRequest(DateTime.UtcNow.Date.AddDays(3), 0, null), null));
            Assert.Equal("INVALID_STATUS", ex.Code);
        }
        Assert.Equal(CarStatus.SanSang, await CarStatusAsync(carId));
    }

    [LocalDbFact]
    public async Task Return_Twice_SecondThrowsReturnExists()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(id);
        var req = new ReturnBookingRequest(DateTime.UtcNow.Date.AddDays(3), 0, null);
        await using (var db = NewContext()) await NewRental(db).ReturnAsync(id, req, null);
        await using (var db = NewContext())
        {
            var ex = await Assert.ThrowsAsync<ConflictException>(() => NewRental(db).ReturnAsync(id, req, null));
            Assert.Equal("RETURN_EXISTS", ex.Code);
        }
    }

    [LocalDbFact]
    public async Task Return_WhenCarWasMovedToMaintenance_DoesNotForceSanSang()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(id);
        await using (var db = NewContext())
        {
            var car = await db.Cars.SingleAsync(c => c.Id == carId);
            car.Status = CarStatus.BaoTri;
            await db.SaveChangesAsync();
        }
        await using (var db = NewContext())
            await NewRental(db).ReturnAsync(id, new ReturnBookingRequest(DateTime.UtcNow.Date.AddDays(3), 0, null), null);

        Assert.Equal(CarStatus.BaoTri, await CarStatusAsync(carId));
    }

    [LocalDbFact]
    public async Task Payment_OnPendingOrCancelledBooking_Rejected()
    {
        var (carId, custId, _) = await SeedAsync();
        int pendingId, cancelledId;
        await using (var db = NewContext())
        {
            var svc = NewBookingService(db);
            pendingId = (await svc.CreateAsync(custId, Req(carId, 1, 3))).Id;
            cancelledId = (await svc.CreateAsync(custId, Req(carId, 5, 7))).Id;
            await svc.CancelAsync(cancelledId, "test");
        }
        await using (var db = NewContext())
        {
            var rental = NewRental(db);
            var req = new CreatePaymentRequest(100_000, PaymentMethod.TienMat, null);
            Assert.Equal("INVALID_STATUS", (await Assert.ThrowsAsync<ConflictException>(() => rental.CreatePaymentAsync(pendingId, req, custId))).Code);
            Assert.Equal("INVALID_STATUS", (await Assert.ThrowsAsync<ConflictException>(() => rental.CreatePaymentAsync(cancelledId, req, custId))).Code);
        }
    }

    [LocalDbFact]
    public async Task Payment_ExceedingDue_Rejected_AndWrongOwnerForbidden()
    {
        var (carId, custId, otherId) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId); // tổng 1.800.000
        await using var db = NewContext();
        var rental = NewRental(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            rental.CreatePaymentAsync(id, new CreatePaymentRequest(1_800_001, PaymentMethod.TienMat, null), custId));
        Assert.Equal("PAYMENT_EXCEEDS_DUE", ex.Code);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            rental.CreatePaymentAsync(id, new CreatePaymentRequest(100_000, PaymentMethod.TienMat, null), otherId));
        await Assert.ThrowsAsync<ForbiddenException>(() => rental.GetPaymentsAsync(id, otherId));
        await Assert.ThrowsAsync<ForbiddenException>(() => rental.GetReturnAsync(id, otherId));
    }

    [LocalDbFact]
    public async Task MarkPaid_InvalidStatusOrAlreadyPaid_Rejected()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId);
        await using var db = NewContext();
        var rental = NewRental(db);
        var p = await rental.CreatePaymentAsync(id, new CreatePaymentRequest(500_000, PaymentMethod.TienMat, null), custId);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            rental.MarkPaymentPaidAsync(p.Id, new MarkPaymentPaidRequest(PaymentStatus.DaHoanTien, null)));
        await rental.MarkPaymentPaidAsync(p.Id, new MarkPaymentPaidRequest(PaymentStatus.DaCoc, null));
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            rental.MarkPaymentPaidAsync(p.Id, new MarkPaymentPaidRequest(PaymentStatus.DaThanhToan, null)));
        Assert.Equal("INVALID_STATUS", ex.Code);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            rental.MarkPaymentPaidAsync(int.MaxValue, new MarkPaymentPaidRequest(PaymentStatus.DaCoc, null)));
    }

    [LocalDbFact]
    public async Task Contract_RealPdf_AtEachStage_AndAccessRules()
    {
        var (carId, custId, otherId) = await SeedAsync();
        int pendingId;
        await using (var db = NewContext())
            pendingId = (await NewBookingService(db).CreateAsync(custId, Req(carId, 10, 12))).Id;
        var id = await ConfirmedBookingAsync(carId, custId, 1, 3);

        ContractFile f1;
        await using (var db = NewContext())
        {
            var rental = NewRental(db);

            f1 = await rental.GetContractAsync(id, custId);
            AssertPdf(f1.Content);
            Assert.Equal($"hop-dong-{id}.pdf", f1.FileName);

            await Assert.ThrowsAsync<ForbiddenException>(() => rental.GetContractAsync(id, otherId));
            Assert.Equal("INVALID_STATUS", (await Assert.ThrowsAsync<ConflictException>(() => rental.GetContractAsync(pendingId, custId))).Code);
            AssertPdf((await rental.GetContractAsync(id, null)).Content); // Staff/Admin
        }

        await using (var db = NewContext()) await NewRental(db).PickupAsync(id);
        await using (var db = NewContext())
            AssertPdf((await NewRental(db).GetContractAsync(id, custId)).Content);

        await using (var db = NewContext())
            await NewRental(db).ReturnAsync(id, new ReturnBookingRequest(DateTime.UtcNow.Date.AddDays(4), 100_000, "Trầy xước"), 1);
        await using (var db = NewContext())
        {
            var f3 = await NewRental(db).GetContractAsync(id, custId);
            AssertPdf(f3.Content);
            Assert.True(f3.Content.Length > f1.Content.Length, "hợp đồng sau trả xe phải có thêm khối kết quả");
        }

        static void AssertPdf(byte[] b)
        {
            Assert.True(b.Length > 0);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(b, 0, 4));
        }
    }

    [LocalDbFact]
    public async Task Contract_Cancelled_Throws409()
    {
        var (carId, custId, _) = await SeedAsync();
        await using var db = NewContext();
        var svc = NewBookingService(db);
        var id = (await svc.CreateAsync(custId, Req(carId, 1, 3))).Id;
        await svc.CancelAsync(id, "x");
        var ex = await Assert.ThrowsAsync<ConflictException>(() => NewRental(db).GetContractAsync(id, custId));
        Assert.Equal("INVALID_STATUS", ex.Code);
    }

    /// <summary>
    /// RỦI RO NGHIỆP VỤ (mô tả hành vi HIỆN TẠI, không phải khẳng định đúng): nhận xe đặt Car.Status=DangThue,
    /// BookingService.CreateAsync yêu cầu Car.Status==SanSang nên không thể đặt trước xe cho NGÀY SAU trong khi xe đang cho thuê,
    /// dù khoảng ngày không trùng. Nếu nghiệp vụ muốn đặt trước, test này sẽ đỏ sau khi sửa - đó là chủ ý.
    /// </summary>
    [LocalDbFact]
    public async Task KnownBehavior_WhileCarIsDangThue_FutureNonOverlappingBookingIsRejected()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId, 1, 3);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(id);

        await using var db2 = NewContext();
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            NewBookingService(db2).CreateAsync(custId, Req(carId, 30, 32)));
        Assert.Equal("CAR_NOT_AVAILABLE", ex.Code);
    }

    [LocalDbFact]
    public async Task KnownBehavior_AfterReturn_CarBookableAgain()
    {
        var (carId, custId, _) = await SeedAsync();
        var id = await ConfirmedBookingAsync(carId, custId, 1, 3);
        await using (var db = NewContext()) await NewRental(db).PickupAsync(id);
        await using (var db = NewContext())
            await NewRental(db).ReturnAsync(id, new ReturnBookingRequest(DateTime.UtcNow.Date.AddDays(3), 0, null), null);

        await using var db2 = NewContext();
        var dto = await NewBookingService(db2).CreateAsync(custId, Req(carId, 30, 32));
        Assert.True(dto.Id > 0);
    }

    private sealed class FakeAi : IAiPricingClient
    {
        public Task<PredictPriceResult> PredictPriceAsync(PredictPriceRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PredictPriceResult(900_000, IsFallback: false));
    }
}
