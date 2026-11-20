using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Audit;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;
using CarRental.Infrastructure.Security;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

public sealed class ApiFixture : TempDb
{
    public const string Password = "Passw0rd!";
    public ApiFactory? Factory { get; private set; }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        if (Available) Factory = new ApiFactory(Conn);
    }

    public override async Task DisposeAsync()
    {
        Factory?.Dispose();
        await base.DisposeAsync();
    }

    public async Task<(int Id, string Email)> SeedUser(UserRole role, string? cccd = null, string? phone = null, bool locked = false)
    {
        await using var db = NewContext();
        var c = new Customer
        {
            FullName = role + " test", Email = Guid.NewGuid().ToString("N") + "@t.vn", PhoneNumber = phone ?? Guid.NewGuid().ToString("N")[..11],
            PasswordHash = new BCryptPasswordHasher().Hash(Password), Role = role, IdentityNumber = cccd, IsLocked = locked
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return (c.Id, c.Email);
    }

    public async Task<int> SeedCar(CarStatus status = CarStatus.SanSang, decimal price = 700_000)
    {
        await using var db = NewContext();
        var car = new Car
        {
            CarType = CarType.Sedan, Brand = "Toyota", Model = "Vios", LicensePlate = "51A-" + Guid.NewGuid().ToString("N")[..8],
            Seats = 5, CarAgeYears = 1, BasePricePerDay = price, Status = status
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.Id;
    }

    public HttpClient Anonymous() => Factory!.CreateClient();

    public async Task<HttpClient> LoginAs(string email)
    {
        var c = Factory!.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = Password });
        r.EnsureSuccessStatusCode();
        var token = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("Token").GetString();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    public async Task<HttpClient> As(UserRole role) => await LoginAs((await SeedUser(role)).Email);
}

public sealed class Wave1ApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fx;
    public Wave1ApiTests(ApiFixture fx) => _fx = fx;

    private static string Plate() => "T-" + Guid.NewGuid().ToString("N")[..8];

    private static object CarBody(string? plate = null, decimal price = 700_000) => new
    {
        CarType = 1, Brand = "Toyota", Model = "Camry", LicensePlate = plate ?? Plate(), Seats = 5, CarAgeYears = 1,
        BasePricePerDay = price, Description = "d"
    };

    private static object UpdateBody(int status, decimal price, decimal? over, string plate, string desc = "d") => new
    {
        CarType = 1, Brand = "Toyota", Model = "Camry", LicensePlate = plate, Seats = 5, CarAgeYears = 1,
        BasePricePerDay = price, Status = status, OverridePrice = over, Description = desc
    };

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<string?> Code(HttpResponseMessage r)
    {
        var j = await Json(r);
        return j.TryGetProperty("code", out var c) ? c.GetString() : null;
    }

    private static int StatusOf(JsonElement car) => car.GetProperty("Status").ValueKind == JsonValueKind.Number
        ? car.GetProperty("Status").GetInt32()
        : (int)Enum.Parse<CarStatus>(car.GetProperty("Status").GetString()!);

    private static object BookReq(int carId, int fromDays = 5, int toDays = 7) => new
    {
        CarId = carId, StartDate = DateTime.UtcNow.Date.AddDays(fromDays), EndDate = DateTime.UtcNow.Date.AddDays(toDays)
    };

    // ---------- Cars ----------

    [LocalDbFact]
    public async Task Staff_CreateCar_IsChoDuyet_AndAuditLogged()
    {
        var staff = await _fx.SeedUser(UserRole.Staff);
        var c = await _fx.LoginAs(staff.Email);
        var r = await c.PostAsJsonAsync("/api/cars", CarBody());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var car = await Json(r);
        Assert.Equal((int)CarStatus.ChoDuyet, StatusOf(car));

        await using var db = _fx.NewContext();
        var log = await db.Set<AuditLog>().SingleAsync(a => a.EntityType == "Car" && a.EntityId == car.GetProperty("Id").GetInt32().ToString() && a.Action == "Car.Created");
        Assert.Equal(staff.Id, log.ActorId);
        Assert.Equal("Staff", log.ActorRole);
    }

    [LocalDbFact]
    public async Task Admin_CreateCar_IsSanSang()
    {
        var r = await (await _fx.As(UserRole.Admin)).PostAsJsonAsync("/api/cars", CarBody());
        Assert.Equal((int)CarStatus.SanSang, StatusOf(await Json(r)));
    }

    [LocalDbFact]
    public async Task Customer_And_Anonymous_CannotCreateCar()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _fx.As(UserRole.Customer)).PostAsJsonAsync("/api/cars", CarBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().PostAsJsonAsync("/api/cars", CarBody())).StatusCode);
    }

    [LocalDbFact]
    public async Task PendingCar_HiddenFromPublic_VisibleToStaffAndAdmin()
    {
        var id = await _fx.SeedCar(CarStatus.ChoDuyet);
        var anon = _fx.Anonymous();

        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync($"/api/cars/{id}")).StatusCode);
        var customer = await _fx.As(UserRole.Customer);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"/api/cars/{id}")).StatusCode);

        foreach (var url in new[] { "/api/cars?PageSize=100", "/api/cars?PageSize=100&IncludePending=true" })
        {
            var list = await Json(await anon.GetAsync(url));
            Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(), x => x.GetProperty("Id").GetInt32() == id);
        }

        foreach (var role in new[] { UserRole.Staff, UserRole.Admin })
        {
            var c = await _fx.As(role);
            Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/cars/{id}")).StatusCode);
            var list = await Json(await c.GetAsync("/api/cars?PageSize=100"));
            Assert.Contains(list.GetProperty("items").EnumerateArray(), x => x.GetProperty("Id").GetInt32() == id);
        }
    }

    [LocalDbFact]
    public async Task PendingCar_CannotBeBooked()
    {
        var id = await _fx.SeedCar(CarStatus.ChoDuyet);
        var r = await (await _fx.As(UserRole.Customer)).PostAsJsonAsync("/api/bookings", BookReq(id));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("CAR_NOT_AVAILABLE", await Code(r));
    }

    [LocalDbFact]
    public async Task Staff_Update_ChangePrice_403_PriceAdminOnly_ButDescriptionOk()
    {
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);
        var car = await Json(await admin.PostAsJsonAsync("/api/cars", CarBody(price: 700_000)));
        var id = car.GetProperty("Id").GetInt32();
        var plate = car.GetProperty("LicensePlate").GetString()!;

        var bad = await staff.PutAsJsonAsync($"/api/cars/{id}", UpdateBody(1, 800_000, null, plate));
        Assert.Equal(HttpStatusCode.Forbidden, bad.StatusCode);
        Assert.Equal("PRICE_ADMIN_ONLY", await Code(bad));

        var badOver = await staff.PutAsJsonAsync($"/api/cars/{id}", UpdateBody(1, 700_000, 500_000, plate));
        Assert.Equal("PRICE_ADMIN_ONLY", await Code(badOver));

        var ok = await staff.PutAsJsonAsync($"/api/cars/{id}", UpdateBody(1, 700_000, null, plate, "mô tả mới"));
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        var adminPrice = await admin.PutAsJsonAsync($"/api/cars/{id}", UpdateBody(1, 800_000, 750_000, plate));
        Assert.Equal(HttpStatusCode.NoContent, adminPrice.StatusCode);
    }

    [LocalDbFact]
    public async Task Staff_Update_Status_ToAndFromPending_403()
    {
        var staff = await _fx.As(UserRole.Staff);
        var okCar = await _fx.SeedCar(CarStatus.SanSang);
        var pendingCar = await _fx.SeedCar(CarStatus.ChoDuyet);
        await using var db = _fx.NewContext();
        string PlateOf(int id) => db.Cars.AsNoTracking().Single(c => c.Id == id).LicensePlate;

        var into = await staff.PutAsJsonAsync($"/api/cars/{okCar}", UpdateBody((int)CarStatus.ChoDuyet, 700_000, null, PlateOf(okCar)));
        Assert.Equal(HttpStatusCode.Forbidden, into.StatusCode);
        Assert.Equal("CAR_APPROVAL_ADMIN_ONLY", await Code(into));

        var outOf = await staff.PutAsJsonAsync($"/api/cars/{pendingCar}", UpdateBody((int)CarStatus.SanSang, 700_000, null, PlateOf(pendingCar)));
        Assert.Equal(HttpStatusCode.Forbidden, outOf.StatusCode);
        Assert.Equal("CAR_APPROVAL_ADMIN_ONLY", await Code(outOf));
    }

    [LocalDbFact]
    public async Task Approve_Authorization()
    {
        var id = await _fx.SeedCar(CarStatus.ChoDuyet);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().PostAsync($"/api/cars/{id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _fx.As(UserRole.Customer)).PostAsync($"/api/cars/{id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _fx.As(UserRole.Staff)).PostAsync($"/api/cars/{id}/approve", null)).StatusCode);
    }

    [LocalDbFact]
    public async Task Approve_NotPending_409_CarNotPending()
    {
        var id = await _fx.SeedCar(CarStatus.SanSang);
        var r = await (await _fx.As(UserRole.Admin)).PostAsync($"/api/cars/{id}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("CAR_NOT_PENDING", await Code(r));
    }

    [LocalDbFact]
    public async Task Approve_NotFound_404()
        => Assert.Equal(HttpStatusCode.NotFound, (await (await _fx.As(UserRole.Admin)).PostAsync("/api/cars/99999999/approve", null)).StatusCode);

    [LocalDbFact]
    public async Task Approve_InvalidPrice_400_AndCarStaysPending()
    {
        var id = await _fx.SeedCar(CarStatus.ChoDuyet);
        var admin = await _fx.As(UserRole.Admin);
        foreach (var body in new object[] { new { BasePricePerDay = 0 }, new { BasePricePerDay = -1 }, new { OverridePrice = 0 } })
        {
            var r = await admin.PostAsJsonAsync($"/api/cars/{id}/approve", body);
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Equal("INVALID_PRICE", await Code(r));
        }
        await using var db = _fx.NewContext();
        Assert.Equal(CarStatus.ChoDuyet, (await db.Cars.SingleAsync(c => c.Id == id)).Status);
    }

    [LocalDbFact]
    public async Task Approve_WithoutBody_KeepsPrice_And_WithPrice_AppliesPrice()
    {
        var admin = await _fx.As(UserRole.Admin);
        var a = await _fx.SeedCar(CarStatus.ChoDuyet, 600_000);
        var r1 = await admin.PostAsync($"/api/cars/{a}/approve", null);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var j1 = await Json(r1);
        Assert.Equal((int)CarStatus.SanSang, StatusOf(j1));
        Assert.Equal(600_000, j1.GetProperty("BasePricePerDay").GetDecimal());

        var b = await _fx.SeedCar(CarStatus.ChoDuyet, 600_000);
        var r2 = await admin.PostAsJsonAsync($"/api/cars/{b}/approve", new { BasePricePerDay = 650_000, OverridePrice = 640_000 });
        var j2 = await Json(r2);
        Assert.Equal((int)CarStatus.SanSang, StatusOf(j2));
        Assert.Equal(650_000, j2.GetProperty("BasePricePerDay").GetDecimal());
        Assert.Equal(640_000, j2.GetProperty("OverridePrice").GetDecimal());

        // sau khi duyệt khách thấy và đặt được
        var customer = await _fx.As(UserRole.Customer);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync($"/api/cars/{a}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await customer.PostAsJsonAsync("/api/bookings", BookReq(a))).StatusCode);
    }

    [LocalDbFact]
    public async Task Delete_Staff403_Admin204()
    {
        var id = await _fx.SeedCar();
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _fx.As(UserRole.Staff)).DeleteAsync($"/api/cars/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await (await _fx.As(UserRole.Admin)).DeleteAsync($"/api/cars/{id}")).StatusCode);
    }

    // ---------- Khóa tài khoản / token ----------

    [LocalDbFact]
    public async Task LockedAccount_Login_403_AccountLocked()
    {
        var u = await _fx.SeedUser(UserRole.Customer, locked: true);
        var r = await _fx.Anonymous().PostAsJsonAsync("/api/auth/login", new { Email = u.Email, Password = ApiFixture.Password });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("ACCOUNT_LOCKED", await Code(r));
    }

    [LocalDbFact]
    public async Task LockedAccount_WrongPassword_Still401_NotLeakingLockState()
    {
        var u = await _fx.SeedUser(UserRole.Customer, locked: true);
        var r = await _fx.Anonymous().PostAsJsonAsync("/api/auth/login", new { Email = u.Email, Password = "sai-mat-khau" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [LocalDbFact]
    public async Task Token_OfLockedUser_Gets401_OnNextRequest_AndUnlockRestores()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var client = await _fx.LoginAs(u.Email);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/bookings")).StatusCode);

        var admin = await _fx.As(UserRole.Admin);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/users/{u.Id}/lock", new { Reason = "vi phạm" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bookings")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/users/{u.Id}/unlock", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/bookings")).StatusCode);
    }

    [LocalDbFact]
    public async Task Token_OfDeletedUser_Gets401()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var client = await _fx.LoginAs(u.Email);
        await using (var db = _fx.NewContext())
        {
            db.Customers.Remove(await db.Customers.SingleAsync(c => c.Id == u.Id));
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bookings")).StatusCode);
    }

    [LocalDbFact]
    public async Task Token_AfterRoleChange_Gets401_EvenForPrivilegeEscalationAndDemotion()
    {
        var staff = await _fx.SeedUser(UserRole.Staff);
        var staffClient = await _fx.LoginAs(staff.Email);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/api/admin/blacklist")).StatusCode);

        var admin = await _fx.As(UserRole.Admin);
        var r = await admin.PutAsJsonAsync($"/api/admin/users/{staff.Id}/role", new { Role = (int)UserRole.Customer });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staffClient.GetAsync("/api/admin/blacklist")).StatusCode);
    }

    [LocalDbFact]
    public async Task Admin_ChangeOwnRole_409_SelfChange()
    {
        var a = await _fx.SeedUser(UserRole.Admin);
        var c = await _fx.LoginAs(a.Email);
        var r = await c.PutAsJsonAsync($"/api/admin/users/{a.Id}/role", new { Role = (int)UserRole.Staff });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("SELF_CHANGE", await Code(r));
    }

    [LocalDbFact]
    public async Task Admin_Users_List_DoesNotExposePasswordHash_AndCreateStaffDuplicates()
    {
        var admin = await _fx.As(UserRole.Admin);
        var raw = await (await admin.GetAsync("/api/admin/users?PageSize=100")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("PasswordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$2", raw);

        var email = Guid.NewGuid().ToString("N") + "@t.vn";
        var phone = "09" + Random.Shared.Next(10_000_000, 99_999_999);
        var body = new { FullName = "S", Email = email, PhoneNumber = phone, Password = "12345678" };
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/users", body)).StatusCode);
        var dupEmail = await admin.PostAsJsonAsync("/api/admin/users", body);
        Assert.Equal(HttpStatusCode.Conflict, dupEmail.StatusCode);
        Assert.Equal("EMAIL_EXISTS", await Code(dupEmail));
        var dupPhone = await admin.PostAsJsonAsync("/api/admin/users", new { FullName = "S", Email = "x" + email, PhoneNumber = phone, Password = "12345678" });
        Assert.Equal("PHONE_EXISTS", await Code(dupPhone));
    }

    // ---------- Blacklist ----------

    private async Task<int> CreateEntry(HttpClient admin, object body)
    {
        var r = await admin.PostAsJsonAsync("/api/admin/blacklist", body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("Id").GetInt32();
    }

    [LocalDbFact]
    public async Task Blocked_Customer_CannotCreateBooking_403_CustomerBlocked_ReasonHidden()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var admin = await _fx.As(UserRole.Admin);
        await CreateEntry(admin, new { CustomerId = u.Id, Level = (int)BlacklistLevel.Chan, Reason = "LÝ-DO-NỘI-BỘ" });
        var car = await _fx.SeedCar();

        var r = await (await _fx.LoginAs(u.Email)).PostAsJsonAsync("/api/bookings", BookReq(car));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("CUSTOMER_BLOCKED", await Code(r));
        Assert.DoesNotContain("LÝ-DO-NỘI-BỘ", await r.Content.ReadAsStringAsync());
    }

    [LocalDbFact]
    public async Task CanhBao_Customer_CanStillBook()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        await CreateEntry(await _fx.As(UserRole.Admin), new { CustomerId = u.Id, Level = (int)BlacklistLevel.CanhBao, Reason = "nhắc" });
        var car = await _fx.SeedCar();
        var r = await (await _fx.LoginAs(u.Email)).PostAsJsonAsync("/api/bookings", BookReq(car));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [LocalDbFact]
    public async Task Block_ByIdentityOrPhone_AppliesToDifferentAccount()
    {
        var admin = await _fx.As(UserRole.Admin);
        var cccd = Guid.NewGuid().ToString("N")[..12];
        var phone = "08" + Guid.NewGuid().ToString("N")[..9];
        await CreateEntry(admin, new { IdentityNumber = cccd, Level = (int)BlacklistLevel.Chan, Reason = "r" });
        await CreateEntry(admin, new { PhoneNumber = phone, Level = (int)BlacklistLevel.Chan, Reason = "r" });
        var byCccd = await _fx.SeedUser(UserRole.Customer, cccd: cccd);
        var byPhone = await _fx.SeedUser(UserRole.Customer, phone: phone);
        var car = await _fx.SeedCar();
        foreach (var u in new[] { byCccd, byPhone })
        {
            var r = await (await _fx.LoginAs(u.Email)).PostAsJsonAsync("/api/bookings", BookReq(car));
            Assert.Equal("CUSTOMER_BLOCKED", await Code(r));
        }
    }

    [LocalDbFact]
    public async Task Confirm_ExistingBooking_OfBlockedCustomer_409_AndOfCanhBaoOk()
    {
        var admin = await _fx.As(UserRole.Admin);
        var blocked = await _fx.SeedUser(UserRole.Customer);
        var warned = await _fx.SeedUser(UserRole.Customer);
        var car1 = await _fx.SeedCar();
        var car2 = await _fx.SeedCar();
        var b1 = (await Json(await (await _fx.LoginAs(blocked.Email)).PostAsJsonAsync("/api/bookings", BookReq(car1)))).GetProperty("Id").GetInt32();
        var b2 = (await Json(await (await _fx.LoginAs(warned.Email)).PostAsJsonAsync("/api/bookings", BookReq(car2)))).GetProperty("Id").GetInt32();
        await CreateEntry(admin, new { CustomerId = blocked.Id, Level = (int)BlacklistLevel.Chan, Reason = "r" });
        await CreateEntry(admin, new { CustomerId = warned.Id, Level = (int)BlacklistLevel.CanhBao, Reason = "r" });

        var staff = await _fx.As(UserRole.Staff);
        var r1 = await staff.PostAsync($"/api/bookings/{b1}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, r1.StatusCode);
        Assert.Equal("CUSTOMER_BLOCKED", await Code(r1));
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/bookings/{b2}/confirm", null)).StatusCode);

        await using var db = _fx.NewContext();
        Assert.Equal(BookingStatus.ChoXacNhan, (await db.Bookings.SingleAsync(b => b.Id == b1)).Status);
    }

    [LocalDbFact]
    public async Task HighestLevelWins_And_RemoveRestoresBooking()
    {
        var admin = await _fx.As(UserRole.Admin);
        var u = await _fx.SeedUser(UserRole.Customer);
        await CreateEntry(admin, new { CustomerId = u.Id, Level = (int)BlacklistLevel.CanhBao, Reason = "a" });
        var chan = await CreateEntry(admin, new { CustomerId = u.Id, Level = (int)BlacklistLevel.Chan, Reason = "b" });
        var car = await _fx.SeedCar();
        var client = await _fx.LoginAs(u.Email);

        var check = await Json(await admin.GetAsync($"/api/admin/blacklist/check?customerId={u.Id}"));
        Assert.Equal((int)BlacklistLevel.Chan, check.GetProperty("Level").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/bookings", BookReq(car))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/blacklist/{chan}/remove", new { Reason = "ok" })).StatusCode);
        // còn mục CanhBao nên vẫn đặt được
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/bookings", BookReq(car))).StatusCode);

        var second = await admin.PostAsJsonAsync($"/api/admin/blacklist/{chan}/remove", new { Reason = "lần 2" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("ALREADY_REMOVED", await Code(second));

        await using var db = _fx.NewContext();
        var row = await db.Set<BlacklistEntry>().SingleAsync(e => e.Id == chan);
        Assert.NotNull(row.RemovedAt);
        Assert.False(row.IsDeleted);
    }

    [LocalDbFact]
    public async Task Blacklist_CreateValidation()
    {
        var admin = await _fx.As(UserRole.Admin);
        var noKey = await admin.PostAsJsonAsync("/api/admin/blacklist", new { Level = 2, Reason = "r" });
        Assert.Equal(HttpStatusCode.BadRequest, noKey.StatusCode);
        Assert.Equal("BLACKLIST_KEY_REQUIRED", await Code(noKey));

        var badLevel = await admin.PostAsJsonAsync("/api/admin/blacklist", new { IdentityNumber = "123456789012", Level = 9, Reason = "r" });
        Assert.Equal("INVALID_LEVEL", await Code(badLevel));

        var noReason = await admin.PostAsJsonAsync("/api/admin/blacklist", new { IdentityNumber = "123456789012", Level = 2, Reason = " " });
        Assert.Equal("REASON_REQUIRED", await Code(noReason));

        var noCustomer = await admin.PostAsJsonAsync("/api/admin/blacklist", new { CustomerId = 99999999, Level = 2, Reason = "r" });
        Assert.Equal(HttpStatusCode.NotFound, noCustomer.StatusCode);

        var removeMissing = await admin.PostAsJsonAsync("/api/admin/blacklist/99999999/remove", new { Reason = "x" });
        Assert.Equal(HttpStatusCode.NotFound, removeMissing.StatusCode);
    }

    // ---------- Phân quyền route mới ----------

    [LocalDbFact]
    public async Task NewRoutes_RoleMatrix()
    {
        var anon = _fx.Anonymous();
        var customer = await _fx.As(UserRole.Customer);
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);

        async Task<HttpStatusCode> Get(HttpClient c, string url) => (await c.GetAsync(url)).StatusCode;
        async Task<HttpStatusCode> Post(HttpClient c, string url) => (await c.PostAsJsonAsync(url, new { })).StatusCode;

        // GET chỉ Admin: users, audit-logs
        foreach (var url in new[] { "/api/admin/users", "/api/admin/audit-logs" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await Get(anon, url));
            Assert.Equal(HttpStatusCode.Forbidden, await Get(customer, url));
            Assert.Equal(HttpStatusCode.Forbidden, await Get(staff, url));
            Assert.Equal(HttpStatusCode.OK, await Get(admin, url));
        }

        // GET blacklist: Staff + Admin
        foreach (var url in new[] { "/api/admin/blacklist", "/api/admin/blacklist/check?customerId=1" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await Get(anon, url));
            Assert.Equal(HttpStatusCode.Forbidden, await Get(customer, url));
            Assert.Equal(HttpStatusCode.OK, await Get(staff, url));
            Assert.Equal(HttpStatusCode.OK, await Get(admin, url));
        }

        // POST/remove blacklist và thao tác users: chỉ Admin (Admin nhận 400/404 vì body rỗng, miễn không 401/403)
        foreach (var url in new[] { "/api/admin/blacklist", "/api/admin/blacklist/99999999/remove", "/api/admin/users",
                     "/api/admin/users/99999999/lock", "/api/admin/users/99999999/unlock" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await Post(anon, url));
            Assert.Equal(HttpStatusCode.Forbidden, await Post(customer, url));
            Assert.Equal(HttpStatusCode.Forbidden, await Post(staff, url));
            var s = await Post(admin, url);
            Assert.True(s != HttpStatusCode.Unauthorized && s != HttpStatusCode.Forbidden, $"{url} -> {s}");
        }
        var putRole = new { Role = 1 };
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync("/api/admin/users/1/role", putRole)).StatusCode);
    }

    [LocalDbFact]
    public async Task AuditLogs_Admin_CanQueryByEntity()
    {
        var admin = await _fx.As(UserRole.Admin);
        var car = await Json(await admin.PostAsJsonAsync("/api/cars", CarBody()));
        var id = car.GetProperty("Id").GetInt32();
        var res = await Json(await admin.GetAsync($"/api/admin/audit-logs?entityType=Car&entityId={id}"));
        var items = res.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, i => i.GetProperty("Action").GetString() == "Car.Created");
        Assert.True(res.GetProperty("total").GetInt32() >= 1);
    }
}
