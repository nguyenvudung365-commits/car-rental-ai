using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Audit;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

/// <summary>Interceptor audit và khớp blacklist trên LocalDB. Mỗi test dùng dữ liệu riêng (GUID) nên dùng chung 1 DB.</summary>
public sealed class AuditAndBlacklistDbTests : IClassFixture<TempDb>
{
    private readonly TempDb _db;
    public AuditAndBlacklistDbTests(TempDb db) => _db = db;

    private static string U() => Guid.NewGuid().ToString("N");

    private static Car NewCar() => new()
    {
        CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + U()[..8],
        Seats = 5, CarAgeYears = 1, BasePricePerDay = 700_000
    };

    private static Customer NewCustomer(string? cccd = null, string? phone = null) => new()
    {
        FullName = "T", Email = U() + "@t.vn", PhoneNumber = phone ?? U()[..11], PasswordHash = "hash-goc", IdentityNumber = cccd
    };

    private async Task<List<AuditLog>> Logs(string entityType, string id)
    {
        await using var db = _db.NewContext();
        return await db.Set<AuditLog>().Where(a => a.EntityType == entityType && a.EntityId == id).OrderBy(a => a.Id).ToListAsync();
    }

    // ---------- Audit ----------

    [LocalDbFact]
    public async Task Audit_CarCreated_HasEntityIdAndActor()
    {
        var actor = new FakeActor { UserId = 5, Role = "Staff" };
        await using var db = _db.NewContext(actor);
        var car = NewCar();
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var logs = await Logs("Car", car.Id.ToString());
        var log = Assert.Single(logs, l => l.Action == "Car.Created");
        Assert.Equal(5, log.ActorId);
        Assert.Equal("Staff", log.ActorRole);
        Assert.Equal("127.0.0.1", log.IpAddress);
        Assert.Contains("Vios", log.Changes);
    }

    [LocalDbFact]
    public async Task Audit_CarStatusUpdate_ActionHasStatusSuffix_AndUpdatedAtIgnored()
    {
        var actor = new FakeActor();
        await using var db = _db.NewContext(actor);
        var car = NewCar();
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        car.Status = CarStatus.BaoTri;
        car.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var upd = Assert.Single(await Logs("Car", car.Id.ToString()), l => l.Action != "Car.Created");
        Assert.Equal("Car.Updated.Status", upd.Action);
        Assert.Contains("\"Status\"", upd.Changes);
        Assert.DoesNotContain("UpdatedAt", upd.Changes);
    }

    [LocalDbFact]
    public async Task Audit_OnlyUpdatedAtChanged_WritesNoLog()
    {
        await using var db = _db.NewContext(new FakeActor());
        var car = NewCar();
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        car.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Assert.Single(await Logs("Car", car.Id.ToString()));
    }

    [LocalDbFact]
    public async Task Audit_PasswordHash_IsMasked_OnCreateAndUpdate()
    {
        await using var db = _db.NewContext(new FakeActor());
        var c = NewCustomer();
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        c.PasswordHash = "hash-moi-bi-mat";
        await db.SaveChangesAsync();

        var logs = await Logs("Customer", c.Id.ToString());
        Assert.Equal(2, logs.Count);
        foreach (var l in logs)
        {
            Assert.Contains("***", l.Changes);
            Assert.DoesNotContain("hash-goc", l.Changes);
            Assert.DoesNotContain("hash-moi-bi-mat", l.Changes);
        }
    }

    [LocalDbFact]
    public async Task Audit_NonWhitelistedEntity_NotLogged()
    {
        await using var db = _db.NewContext(new FakeActor());
        var car = NewCar();
        car.Images.Add(new CarImage { ImageUrl = "cars/a.webp", IsPrimary = true });
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        Assert.False(await db.Set<AuditLog>().AnyAsync(a => a.EntityType == "CarImage"));
        Assert.False(await db.Set<AuditLog>().AnyAsync(a => a.EntityType == "AuditLog"));
    }

    [LocalDbFact]
    public async Task Audit_BlacklistEntryIsLogged()
    {
        await using var db = _db.NewContext(new FakeActor());
        var e = new BlacklistEntry { IdentityNumber = U()[..12], Level = BlacklistLevel.Chan, Reason = "r", CreatedById = 1 };
        db.Set<BlacklistEntry>().Add(e);
        await db.SaveChangesAsync();
        Assert.Single(await Logs("BlacklistEntry", e.Id.ToString()), l => l.Action == "BlacklistEntry.Created");
    }

    [LocalDbFact]
    public async Task Audit_ChangesJson_TruncatedAt8000()
    {
        await using var db = _db.NewContext(new FakeActor());
        var car = NewCar();
        car.Description = new string('x', 20_000);
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var log = Assert.Single(await Logs("Car", car.Id.ToString()));
        Assert.Equal(8000, log.Changes!.Length);
    }

    [LocalDbFact]
    public async Task Audit_NoInterceptor_NoLogs()
    {
        await using var db = _db.NewContext();
        var car = NewCar();
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        Assert.Empty(await Logs("Car", car.Id.ToString()));
    }

    // ---------- Blacklist matching ----------

    private BlacklistService Svc(ApplicationDbContext db) =>
        new(new BlacklistRepository(db), new CustomerRepository(db), db, new FakeActor { UserId = 1 });

    private async Task<int> Block(string? cccd, string? phone, BlacklistLevel level, int? customerId = null)
    {
        await using var db = _db.NewContext();
        var dto = await Svc(db).CreateAsync(new CreateBlacklistRequest(customerId, cccd, phone, level, "lý do"));
        return dto.Id;
    }

    private async Task<int> Save(Customer c)
    {
        await using var db = _db.NewContext();
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    private async Task<ScreeningResult> Screen(int customerId)
    {
        await using var db = _db.NewContext();
        return await Svc(db).ScreenAsync(customerId);
    }

    [LocalDbFact]
    public async Task Match_ByCustomerId()
    {
        var id = await Save(NewCustomer());
        Assert.Null((await Screen(id)).Level);
        await Block(null, null, BlacklistLevel.CanhBao, customerId: id);
        Assert.Equal(BlacklistLevel.CanhBao, (await Screen(id)).Level);
    }

    [LocalDbFact]
    public async Task Match_ByIdentityNumber_OfAnotherAccount()
    {
        var cccd = U()[..12];
        await Block(cccd, null, BlacklistLevel.Chan);
        var newAccount = await Save(NewCustomer(cccd: cccd));
        Assert.Equal(BlacklistLevel.Chan, (await Screen(newAccount)).Level);
    }

    [LocalDbFact]
    public async Task Match_ByPhone_OfAnotherAccount()
    {
        var phone = U()[..11];
        await Block(null, phone, BlacklistLevel.Chan);
        var newAccount = await Save(NewCustomer(phone: phone));
        Assert.Equal(BlacklistLevel.Chan, (await Screen(newAccount)).Level);
    }

    [LocalDbFact]
    public async Task Match_HighestLevelWins_AcrossKeys()
    {
        var cccd = U()[..12];
        var id = await Save(NewCustomer(cccd: cccd));
        await Block(null, null, BlacklistLevel.CanhBao, customerId: id);
        await Block(cccd, null, BlacklistLevel.Chan);
        Assert.Equal(BlacklistLevel.Chan, (await Screen(id)).Level);
    }

    [LocalDbFact]
    public async Task Match_UnrelatedCustomer_NotMatched()
    {
        await Block(U()[..12], U()[..11], BlacklistLevel.Chan);
        var id = await Save(NewCustomer(cccd: U()[..12]));
        Assert.Null((await Screen(id)).Level);
    }

    [LocalDbFact]
    public async Task Remove_SetsRemovedAtOnly_RowKept_NoLongerMatches_SecondRemoveConflicts()
    {
        var cccd = U()[..12];
        var id = await Save(NewCustomer(cccd: cccd));
        var entryId = await Block(cccd, null, BlacklistLevel.Chan);

        await using (var db = _db.NewContext())
            await Svc(db).RemoveAsync(entryId, new RemoveBlacklistRequest("gỡ"));

        Assert.Null((await Screen(id)).Level);
        await using var check = _db.NewContext();
        var row = await check.Set<BlacklistEntry>().SingleAsync(e => e.Id == entryId);
        Assert.NotNull(row.RemovedAt);
        Assert.Equal("gỡ", row.RemoveReason);
        Assert.Equal(1, row.RemovedById);
        Assert.False(row.IsDeleted);
        Assert.Equal(BlacklistLevel.Chan, row.Level);

        var ex = await Assert.ThrowsAsync<CarRental.Application.Common.ConflictException>(() => Svc(check).RemoveAsync(entryId, new RemoveBlacklistRequest(null)));
        Assert.Equal("ALREADY_REMOVED", ex.Code);
    }

    [LocalDbFact]
    public async Task List_ActiveOnlyDefault_ExcludesRemoved()
    {
        var entryId = await Block(U()[..12], null, BlacklistLevel.CanhBao);
        await using (var db = _db.NewContext()) await Svc(db).RemoveAsync(entryId, new RemoveBlacklistRequest("test"));
        await using var q = _db.NewContext();
        var (active, _) = await Svc(q).ListAsync(new BlacklistFilterRequest(null, 1, 100));
        var (all, _) = await Svc(q).ListAsync(new BlacklistFilterRequest(false, 1, 100));
        Assert.DoesNotContain(active, e => e.Id == entryId);
        Assert.Contains(all, e => e.Id == entryId);
    }
}
