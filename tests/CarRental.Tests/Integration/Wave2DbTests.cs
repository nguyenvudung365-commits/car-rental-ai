using CarRental.Application.Modules.Customers;
using CarRental.Application.Modules.Reports;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

/// <summary>Truy vấn/ràng buộc của đợt 2 trên LocalDB thật (TempDb tự migrate nên cũng kiểm tra migration chạy được trên DB sạch).</summary>
public sealed class Wave2DbTests : IClassFixture<TempDb>
{
    private readonly TempDb _db;
    public Wave2DbTests(TempDb db) => _db = db;

    private static string U() => Guid.NewGuid().ToString("N");
    private static string Digits(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => Random.Shared.Next(10)));

    private async Task<Customer> NewCustomer(string? tag = null, UserRole role = UserRole.Customer, string? cccd = null,
        string? phone = null, int lifetime = 0, bool deleted = false, DateTime? created = null)
    {
        await using var db = _db.NewContext();
        var c = new Customer
        {
            FullName = (tag ?? "KH") + " " + U()[..6], Email = U() + "@t.vn", PhoneNumber = phone ?? "09" + U()[..9], PasswordHash = "x",
            Role = role, IdentityNumber = cccd, LifetimePoints = lifetime, LoyaltyPoints = lifetime, IsDeleted = deleted,
            CreatedAt = created ?? DateTime.UtcNow
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    private async Task<int> NewCar(CarStatus status = CarStatus.SanSang)
    {
        await using var db = _db.NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "T-" + U()[..8], Seats = 5, CarAgeYears = 1,
            BasePricePerDay = 700_000, Status = status
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    private async Task<int> NewBooking(int customerId, int carId, BookingStatus status, decimal total = 1_000_000, decimal? finalAmount = null,
        int startOffset = -5, int endOffset = -2)
    {
        await using var db = _db.NewContext();
        var b = new Booking
        {
            CustomerId = customerId, CarId = carId, Status = status, TotalPrice = total, FinalPricePerDay = total / 3,
            StartDate = DateTime.UtcNow.Date.AddDays(startOffset), EndDate = DateTime.UtcNow.Date.AddDays(endOffset)
        };
        if (finalAmount is { } f) b.ReturnRecord = new ReturnRecord { FinalAmount = f, ReturnedAt = b.EndDate };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    private async Task AddDoc(int customerId, DocumentType type, DocumentStatus status, bool deleted = false)
    {
        await using var db = _db.NewContext();
        db.Set<CustomerDocument>().Add(new CustomerDocument
        {
            CustomerId = customerId, Type = type, Status = status, Number = "012345678901", FrontKey = "private/x.webp", IsDeleted = deleted,
            ExpiryDate = type == DocumentType.BangLai ? DateTime.UtcNow.AddYears(1) : null
        });
        await db.SaveChangesAsync();
    }

    private async Task Blacklist(int? customerId, string? cccd = null, string? phone = null, bool removed = false)
    {
        await using var db = _db.NewContext();
        db.Set<BlacklistEntry>().Add(new BlacklistEntry
        {
            CustomerId = customerId, IdentityNumber = cccd, PhoneNumber = phone, Level = BlacklistLevel.Chan, Reason = "r", CreatedById = 1,
            RemovedAt = removed ? DateTime.UtcNow : null
        });
        await db.SaveChangesAsync();
    }

    private async Task<(List<CustomerListRow> Items, int Total)> List(string search, MemberTier? tier = null,
        DocumentsOverallStatus? doc = null, bool? bl = null, int page = 1, int size = 50)
    {
        await using var db = _db.NewContext();
        return await new CustomerAdminRepository(db).SearchAsync(new(search, tier, doc, bl, page, size), page, size);
    }

    // ---------- Migration ----------

    [LocalDbFact]
    public async Task Migration_Applied_AndModelHasNoPendingChanges()
    {
        await using var db = _db.NewContext();
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("AddCustomerProfileDocsLoyalty"));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "Model lệch snapshot: thiếu migration.");
        // các cột/bảng mới truy vấn được
        _ = await db.Customers.Select(c => new { c.AvatarKey, c.LoyaltyPoints, c.LifetimePoints }).FirstOrDefaultAsync();
        _ = await db.Set<CustomerDocument>().CountAsync();
        _ = await db.Set<LoyaltyTransaction>().CountAsync();
    }

    [LocalDbFact]
    public async Task UniqueActiveDocumentPerType_ButSoftDeletedAllowed()
    {
        var c = await NewCustomer();
        await AddDoc(c.Id, DocumentType.Cccd, DocumentStatus.DaDuyet, deleted: true);
        await AddDoc(c.Id, DocumentType.Cccd, DocumentStatus.ChoDuyet);            // bản cũ đã xóa mềm + 1 bản hiệu lực: OK
        await AddDoc(c.Id, DocumentType.BangLai, DocumentStatus.ChoDuyet);          // loại khác: OK
        await Assert.ThrowsAsync<DbUpdateException>(() => AddDoc(c.Id, DocumentType.Cccd, DocumentStatus.ChoDuyet));
    }

    [LocalDbFact]
    public async Task LoyaltyUnique_BookingAndReason_ButAdjustmentsWithoutBookingRepeat()
    {
        var c = await NewCustomer();
        var car = await NewCar();
        var bk = await NewBooking(c.Id, car, BookingStatus.DaTraXe);
        async Task Add(int? booking, LoyaltyReason r)
        {
            await using var db = _db.NewContext();
            db.Set<LoyaltyTransaction>().Add(new LoyaltyTransaction { CustomerId = c.Id, BookingId = booking, Points = 1, Reason = r });
            await db.SaveChangesAsync();
        }
        await Add(bk, LoyaltyReason.TichDiem);
        await Add(bk, LoyaltyReason.DieuChinh);          // khác Reason: được
        await Assert.ThrowsAsync<DbUpdateException>(() => Add(bk, LoyaltyReason.TichDiem));
        await Add(null, LoyaltyReason.DieuChinh);
        await Add(null, LoyaltyReason.DieuChinh);        // BookingId null: không bị unique
    }

    // ---------- LoyaltyService trên DB thật ----------

    private LoyaltyService Loyalty(ApplicationDbContext db) => new(new CustomerRepository(db), new LoyaltyRepository(db), db);

    [LocalDbFact]
    public async Task Award_OnRealDb_AddsOnce_SecondCallNoChange_TierUpdates()
    {
        var c = await NewCustomer(lifetime: 99_000);
        var car = await NewCar();
        var bk = await NewBooking(c.Id, car, BookingStatus.DaTraXe);
        for (var i = 0; i < 2; i++)
        {
            await using var db = _db.NewContext();
            await Loyalty(db).AwardForBookingAsync(c.Id, bk, 2_100_000m);
        }
        await using var check = _db.NewContext();
        var fresh = await check.Customers.SingleAsync(x => x.Id == c.Id);
        Assert.Equal(99_000 + 21_000, fresh.LoyaltyPoints);
        Assert.Equal(120_000, fresh.LifetimePoints);
        Assert.Equal(1, await check.Set<LoyaltyTransaction>().CountAsync(t => t.BookingId == bk && t.Reason == LoyaltyReason.TichDiem));
        await using var db2 = _db.NewContext();
        Assert.Equal(MemberTier.Bac, (await Loyalty(db2).GetAsync(c.Id)).Tier);
    }

    // Kế hoạch: "hàm idempotent: lỗi trùng unique thì bỏ qua". Hai request đồng thời cho cùng đơn không được ném lỗi.
    [LocalDbFact]
    public async Task Award_Concurrent_SameBooking_NoException_ExactlyOnce()
    {
        var c = await NewCustomer();
        var car = await NewCar();
        var bk = await NewBooking(c.Id, car, BookingStatus.DaTraXe);
        var tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = _db.NewContext();
            await Loyalty(db).AwardForBookingAsync(c.Id, bk, 2_100_000m);
        });
        await Task.WhenAll(tasks);
        await using var check = _db.NewContext();
        Assert.Equal(1, await check.Set<LoyaltyTransaction>().CountAsync(t => t.BookingId == bk));
        Assert.Equal(21_000, (await check.Customers.SingleAsync(x => x.Id == c.Id)).LoyaltyPoints);
    }

    [LocalDbFact]
    public async Task Adjust_OnRealDb_NegativeBalanceRejected_NothingPersisted()
    {
        var c = await NewCustomer(lifetime: 50);
        await using var db = _db.NewContext();
        await Assert.ThrowsAsync<CarRental.Application.Common.BadRequestException>(() => Loyalty(db).AdjustAsync(c.Id, new(-51, "x")));
        await using var db2 = _db.NewContext();
        var dto = await Loyalty(db2).AdjustAsync(c.Id, new(-50, "đổi quà"));
        Assert.Equal(0, dto.Points);
        Assert.Equal(50, dto.LifetimePoints);
        Assert.Single(dto.History);
    }

    // ---------- CustomerAdminRepository ----------

    [LocalDbFact]
    public async Task List_OnlyRoleCustomer_ExcludesStaffAdminAndDeleted()
    {
        var tag = "ONLYCUST" + U()[..6];
        await NewCustomer(tag);
        await NewCustomer(tag, UserRole.Staff);
        await NewCustomer(tag, UserRole.Admin);
        await NewCustomer(tag, deleted: true);
        var (items, total) = await List(tag);
        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal(UserRole.Customer, items[0].Customer.Role);
    }

    [LocalDbFact]
    public async Task List_Search_ByNameEmailPhone()
    {
        var tag = "SRCH" + U()[..6];
        var a = await NewCustomer(tag, phone: "0977" + U()[..6]);
        await NewCustomer("khac");
        Assert.Single((await List(tag)).Items);
        Assert.Single((await List(a.Email)).Items);
        Assert.Single((await List(a.PhoneNumber)).Items);
        Assert.Empty((await List("zzzz-khong-co-" + U())).Items);
    }

    [LocalDbFact]
    public async Task List_TierFilter_Boundaries()
    {
        var tag = "TIER" + U()[..6];
        foreach (var p in new[] { 0, 99_999, 100_000, 299_999, 300_000, 799_999, 800_000 }) await NewCustomer(tag, lifetime: p);
        Assert.Equal(2, (await List(tag, MemberTier.ThanhVien)).Total);
        Assert.Equal(2, (await List(tag, MemberTier.Bac)).Total);
        Assert.Equal(2, (await List(tag, MemberTier.Vang)).Total);
        Assert.Equal(1, (await List(tag, MemberTier.KimCuong)).Total);
    }

    [LocalDbFact]
    public async Task List_DocStatusFilter_AndOverallComputation()
    {
        var tag = "DOCS" + U()[..6];
        var none = await NewCustomer(tag);
        var pending = await NewCustomer(tag);
        var rejected = await NewCustomer(tag);
        var approved = await NewCustomer(tag);
        var onlyOne = await NewCustomer(tag);
        var oldDeleted = await NewCustomer(tag);
        await AddDoc(pending.Id, DocumentType.Cccd, DocumentStatus.ChoDuyet);
        await AddDoc(rejected.Id, DocumentType.Cccd, DocumentStatus.DaDuyet);
        await AddDoc(rejected.Id, DocumentType.BangLai, DocumentStatus.TuChoi);
        await AddDoc(approved.Id, DocumentType.Cccd, DocumentStatus.DaDuyet);
        await AddDoc(approved.Id, DocumentType.BangLai, DocumentStatus.DaDuyet);
        await AddDoc(onlyOne.Id, DocumentType.Cccd, DocumentStatus.DaDuyet);
        await AddDoc(oldDeleted.Id, DocumentType.Cccd, DocumentStatus.TuChoi, deleted: true);   // bản xóa mềm không tính

        async Task<List<int>> Ids(DocumentsOverallStatus s) => (await List(tag, doc: s)).Items.Select(i => i.Customer.Id).OrderBy(i => i).ToList();
        // Chỉ có 1 giấy tờ đã duyệt (thiếu loại kia) vẫn là ChuaNop theo luật Overall của service.
        Assert.Equal(new[] { none.Id, oldDeleted.Id, onlyOne.Id }.OrderBy(i => i), await Ids(DocumentsOverallStatus.ChuaNop));
        Assert.Equal([pending.Id], await Ids(DocumentsOverallStatus.ChoDuyet));
        Assert.Equal([rejected.Id], await Ids(DocumentsOverallStatus.TuChoi));
        Assert.Equal([approved.Id], await Ids(DocumentsOverallStatus.DaDuyet));
        Assert.Equal(DocumentsOverallStatus.ChuaNop, (await List(tag, doc: DocumentsOverallStatus.ChuaNop)).Items.First(i => i.Customer.Id == oldDeleted.Id).DocumentsStatus);
    }

    [LocalDbFact]
    public async Task List_BlacklistFilter_ByCustomerId_Cccd_Phone_RemovedIgnored()
    {
        var tag = "BL" + U()[..6];
        var byId = await NewCustomer(tag);
        var cccd = Digits(12);
        var phone = "0988" + Digits(6);
        var byCccd = await NewCustomer(tag, cccd: cccd);
        var byPhone = await NewCustomer(tag, phone: phone);
        var removed = await NewCustomer(tag);
        var clean = await NewCustomer(tag);
        await Blacklist(byId.Id);
        await Blacklist(null, cccd: cccd);
        await Blacklist(null, phone: phone);
        await Blacklist(removed.Id, removed: true);

        var yes = (await List(tag, bl: true)).Items.Select(i => i.Customer.Id).OrderBy(i => i).ToList();
        Assert.Equal(new[] { byId.Id, byCccd.Id, byPhone.Id }.OrderBy(i => i), yes);
        var no = (await List(tag, bl: false)).Items.Select(i => i.Customer.Id).OrderBy(i => i).ToList();
        Assert.Equal(new[] { removed.Id, clean.Id }.OrderBy(i => i), no);
        Assert.All((await List(tag, bl: true)).Items, i => Assert.Equal(BlacklistLevel.Chan, i.BlacklistLevel));
    }

    [LocalDbFact]
    public async Task List_BookingStats_ExcludeCancelled_SpentUsesFinalAmountOrTotal()
    {
        var tag = "STAT" + U()[..6];
        var c = await NewCustomer(tag);
        var car = await NewCar();
        await NewBooking(c.Id, car, BookingStatus.DaTraXe, total: 2_000_000, finalAmount: 2_100_000);   // dùng FinalAmount
        await NewBooking(c.Id, car, BookingStatus.DaTraXe, total: 500_000);                              // không có biên bản: TotalPrice
        await NewBooking(c.Id, car, BookingStatus.DaHuy, total: 9_000_000);                              // hủy: không tính
        await NewBooking(c.Id, car, BookingStatus.DaXacNhan, total: 700_000, startOffset: 10, endOffset: 12);   // đếm nhưng chưa tính doanh thu
        var row = (await List(tag)).Items.Single();
        Assert.Equal(3, row.BookingCount);
        Assert.Equal(2_600_000m, row.TotalSpent);

        await using var db = _db.NewContext();
        var (count, spent) = await new CustomerAdminRepository(db).GetStatsAsync(c.Id);
        Assert.Equal((3, 2_600_000m), (count, spent));
    }

    [LocalDbFact]
    public async Task List_NoBookings_ZeroStats_AndPagingOrderedNewestFirst()
    {
        var tag = "PAGE" + U()[..6];
        var ids = new List<int>();
        for (var i = 0; i < 5; i++) ids.Add((await NewCustomer(tag, created: DateTime.UtcNow.AddMinutes(-10 + i))).Id);
        var p1 = await List(tag, page: 1, size: 2);
        var p3 = await List(tag, page: 3, size: 2);
        Assert.Equal(5, p1.Total);
        Assert.Equal([ids[4], ids[3]], p1.Items.Select(i => i.Customer.Id).ToList());
        Assert.Equal([ids[0]], p3.Items.Select(i => i.Customer.Id).ToList());
        Assert.All(p1.Items, i => { Assert.Equal(0, i.BookingCount); Assert.Equal(0m, i.TotalSpent); });
    }

    [LocalDbFact]
    public async Task RecentBookings_Max10_NewestFirst_ExcludesDeleted()
    {
        var c = await NewCustomer();
        var car = await NewCar();
        for (var i = 0; i < 12; i++) await NewBooking(c.Id, car, BookingStatus.DaTraXe, total: 100 + i);
        await using (var db = _db.NewContext())
        {
            var one = await db.Bookings.FirstAsync(b => b.CustomerId == c.Id);
            one.IsDeleted = true;
            await db.SaveChangesAsync();
        }
        await using var db2 = _db.NewContext();
        var recent = await new CustomerAdminRepository(db2).GetRecentBookingsAsync(c.Id, 10);
        Assert.Equal(10, recent.Count);
        Assert.Equal("Toyota Vios", recent[0].CarName);
    }

    // ---------- PHẦN 0 ----------

    private BlacklistService Bl(ApplicationDbContext db) => new(new BlacklistRepository(db), new CustomerRepository(db), db, new FakeActor { UserId = 1 });

    // M1: thêm blacklist theo CustomerId tự điền CCCD/SĐT => tạo tài khoản mới cùng CCCD/SĐT vẫn bị chặn.
    [LocalDbFact]
    public async Task M1_BlacklistByCustomerId_AutofillsIdentity_BlocksNewAccountWithSameCccdOrPhone()
    {
        var cccd = Digits(12);
        var phone = "0933" + Digits(6);
        var victim = await NewCustomer(cccd: cccd, phone: phone);
        await using (var db = _db.NewContext())
        {
            var dto = await Bl(db).CreateAsync(new CreateBlacklistRequest(victim.Id, null, null, BlacklistLevel.Chan, "gian lận"));
            Assert.Equal(cccd, dto.IdentityNumber);
            Assert.Equal(phone, dto.PhoneNumber);
        }
        var sameCccd = await NewCustomer(cccd: cccd);
        var samePhone = await NewCustomer(phone: "+84" + phone[1..]); // cột SĐT unique, nên dùng dạng +84 của cùng số
        var stranger = await NewCustomer();
        await using var db2 = _db.NewContext();
        Assert.Equal(BlacklistLevel.Chan, (await Bl(db2).ScreenAsync(sameCccd.Id)).Level);
        Assert.Equal(BlacklistLevel.Chan, (await Bl(db2).ScreenAsync(samePhone.Id)).Level);
        Assert.Null((await Bl(db2).ScreenAsync(stranger.Id)).Level);
    }

    // M2: +84912000092, "0912 000 092" và 0912000092 cùng khớp.
    [LocalDbFact]
    public async Task M2_PhoneFormats_AllMatchSameBlacklistEntry()
    {
        var suffix = U()[..6].Where(char.IsDigit).Aggregate("", (a, ch) => a + ch).PadRight(6, '7')[..6];
        var local = "0912" + suffix;                      // 10 số
        var intl = "+84" + local[1..];                    // +84912xxxxxx
        var spaced = local[..4] + " " + local[4..7] + " " + local[7..];
        await using (var db = _db.NewContext())
            await Bl(db).CreateAsync(new CreateBlacklistRequest(null, null, spaced, BlacklistLevel.Chan, "r"));

        var a = await NewCustomer(phone: local);
        var b = await NewCustomer(phone: intl);
        var c = await NewCustomer(phone: spaced);
        await using var db2 = _db.NewContext();
        foreach (var cust in new[] { a, b, c })
            Assert.Equal(BlacklistLevel.Chan, (await Bl(db2).ScreenAsync(cust.Id)).Level);
    }
}

/// <summary>DB riêng để số liệu lấp đầy xác định (không lẫn xe của test khác).</summary>
public sealed class Wave2OccupancyDbTests : IClassFixture<TempDb>
{
    private readonly TempDb _db;
    public Wave2OccupancyDbTests(TempDb db) => _db = db;

    private async Task<int> NewCar(CarStatus status)
    {
        await using var db = _db.NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "T-" + Guid.NewGuid().ToString("N")[..8], Seats = 5,
            CarAgeYears = 1, BasePricePerDay = 700_000, Status = status
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    // L5: xe ChoDuyet không tính vào tỷ lệ lấp đầy.
    [LocalDbFact]
    public async Task L5_Occupancy_ExcludesPendingApprovalAndInactiveCars()
    {
        var ok1 = await NewCar(CarStatus.SanSang);
        var ok2 = await NewCar(CarStatus.DangThue);
        await NewCar(CarStatus.ChoDuyet);
        await NewCar(CarStatus.NgungHoatDong);

        var from = DateTime.UtcNow.Date.AddDays(-10);
        int custId;
        await using (var d = _db.NewContext())
        {
            var cu = new Customer { FullName = "x", Email = Guid.NewGuid().ToString("N") + "@t.vn", PhoneNumber = "09" + Guid.NewGuid().ToString("N")[..9], PasswordHash = "x" };
            d.Customers.Add(cu);
            await d.SaveChangesAsync();
            custId = cu.Id;
            d.Bookings.Add(new Booking { CustomerId = custId, CarId = ok1, Status = BookingStatus.DaTraXe, TotalPrice = 1, FinalPricePerDay = 1, StartDate = from, EndDate = from.AddDays(5) });
            await d.SaveChangesAsync();
        }
        await using var db = _db.NewContext();
        var report = await new ReportRepository(db).GetOccupancyAsync(from, DateTime.UtcNow.Date);
        Assert.Equal(2, report.TotalCars);
        Assert.Equal(new[] { ok1, ok2 }.OrderBy(i => i), report.ByCar.Select(c => c.CarId).OrderBy(i => i));
    }
}
