using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;
using CarRental.Domain.Modules.Customers;
using CarRental.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Tests.Integration;

/// <summary>Đợt 2 qua HTTP thật (WebApplicationFactory + LocalDB): phân quyền, luồng giấy tờ, điểm thưởng, mật khẩu, avatar, guard.</summary>
public sealed class Wave2ApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fx;
    public Wave2ApiTests(ApiFixture fx) => _fx = fx;

    private static string Digits(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => Random.Shared.Next(10)));
    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<string?> Code(HttpResponseMessage r)
    {
        var j = await Json(r);
        return j.TryGetProperty("code", out var c) ? c.GetString() : null;
    }

    private static MultipartFormDataContent WebpForm(string field, string fileName = "a.webp", string contentType = "image/webp", byte[]? bytes = null,
        Dictionary<string, string>? fields = null)
    {
        var form = new MultipartFormDataContent();
        foreach (var (k, v) in fields ?? []) form.Add(new StringContent(v), k);
        var file = new ByteArrayContent(bytes ?? WebpBytes.Tiny);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, field, fileName);
        return form;
    }

    private static MultipartFormDataContent DocForm(int type, string number, string? expiry = null, bool back = false, string frontName = "f.webp")
    {
        var form = WebpForm("Front", frontName, fields: new() { ["Type"] = type.ToString(), ["Number"] = number });
        if (expiry is not null) form.Add(new StringContent(expiry), "ExpiryDate");
        if (back)
        {
            var b = new ByteArrayContent(WebpBytes.Tiny);
            b.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
            form.Add(b, "Back", "b.webp");
        }
        return form;
    }

    private static string Future() => DateTime.UtcNow.Date.AddYears(2).ToString("yyyy-MM-dd");

    private async Task<int> Submit(HttpClient c, int type, string? number = null, string? expiry = null)
    {
        var r = await c.PostAsync("/api/me/documents", DocForm(type, number ?? Digits(12), type == 2 ? expiry ?? Future() : null, back: true));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("Id").GetInt32();
    }

    private async Task SeedApprovedDocs(int customerId, DateTime? licenseExpiry = null, DocumentStatus licenseStatus = DocumentStatus.DaDuyet,
        DocumentStatus cccdStatus = DocumentStatus.DaDuyet, bool withCccd = true, bool withLicense = true)
    {
        await using var db = _fx.NewContext();
        if (withCccd)
            db.Set<CustomerDocument>().Add(new CustomerDocument { CustomerId = customerId, Type = DocumentType.Cccd, Status = cccdStatus, Number = Digits(12), FrontKey = "private/x.webp" });
        if (withLicense)
            db.Set<CustomerDocument>().Add(new CustomerDocument
            {
                CustomerId = customerId, Type = DocumentType.BangLai, Status = licenseStatus, Number = Digits(12), FrontKey = "private/y.webp",
                ExpiryDate = licenseExpiry ?? DateTime.UtcNow.Date.AddYears(3)
            });
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedBooking(int customerId, int carId, BookingStatus status, int startOffset, int endOffset, decimal total = 2_100_000)
    {
        await using var db = _fx.NewContext();
        var b = new Booking
        {
            CustomerId = customerId, CarId = carId, Status = status, TotalPrice = total, FinalPricePerDay = total / 3,
            StartDate = DateTime.UtcNow.Date.AddDays(startOffset), EndDate = DateTime.UtcNow.Date.AddDays(endOffset)
        };
        db.Bookings.Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    // Luật đợt 3: nhận xe cần biên bản Giao, trả xe cần biên bản Nhan.
    private async Task GiveHandover(HttpClient staff, int bookingId, HandoverType type, int odo = 1000)
    {
        var r = await staff.PostAsJsonAsync($"/api/bookings/{bookingId}/handovers", new { Type = (int)type, Odometer = odo, FuelLevel = 80, ConditionNote = "ok" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    private async Task SeedGiaoRecord(int bookingId)
    {
        await using var db = _fx.NewContext();
        db.Set<HandoverRecord>().Add(new HandoverRecord { BookingId = bookingId, Type = HandoverType.Giao, Odometer = 1000, FuelLevel = 80 });
        await db.SaveChangesAsync();
    }

    // ---------- /api/auth/me và hồ sơ ----------

    [LocalDbFact]
    public async Task Me_Anonymous401_Customer200_WithAllFields()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().GetAsync("/api/auth/me")).StatusCode);
        var u = await _fx.SeedUser(UserRole.Customer, cccd: "012345678901");
        var r = await (await _fx.LoginAs(u.Email)).GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        foreach (var f in new[] { "Id", "FullName", "Email", "PhoneNumber", "Role", "IdentityNumber", "Address", "AvatarKey", "LoyaltyPoints", "Tier", "DocumentsStatus" })
            Assert.True(j.TryGetProperty(f, out _), "thiếu trường " + f);
        Assert.Equal(u.Id, j.GetProperty("Id").GetInt32());
        Assert.Equal("Customer", j.GetProperty("Role").GetString());
        Assert.DoesNotContain("PasswordHash", await r.Content.ReadAsStringAsync());
    }

    [LocalDbFact]
    public async Task ChangePassword_Flow_WrongOld_Weak_Same_Success_LoginSwitches_OldTokenStillValidByDesign()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var c = await _fx.LoginAs(u.Email);

        var wrong = await c.PostAsJsonAsync("/api/auth/me/password", new { CurrentPassword = "sai-mat-khau", NewPassword = "NewPassw0rd!" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("WRONG_PASSWORD", await Code(wrong));

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/me/password", new { CurrentPassword = ApiFixture.Password, NewPassword = "short7!" })).StatusCode);
        var same = await c.PostAsJsonAsync("/api/auth/me/password", new { CurrentPassword = ApiFixture.Password, NewPassword = ApiFixture.Password });
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.Equal("SAME_PASSWORD", await Code(same));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().PostAsJsonAsync("/api/auth/me/password", new { CurrentPassword = "a", NewPassword = "bbbbbbbb" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/auth/me/password", new { CurrentPassword = ApiFixture.Password, NewPassword = "NewPassw0rd!" })).StatusCode);

        var anon = _fx.Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/auth/login", new { u.Email, Password = ApiFixture.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.PostAsJsonAsync("/api/auth/login", new { u.Email, Password = "NewPassw0rd!" })).StatusCode);
        // Thiết kế hiện tại: token chỉ bị vô hiệu khi khóa/xóa/đổi vai trò, nên token cũ vẫn dùng được sau đổi mật khẩu.
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/auth/me")).StatusCode);
    }

    [LocalDbFact]
    public async Task Avatar_ValidWebp_200_InvalidFormats_400_Anonymous401()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var c = await _fx.LoginAs(u.Email);

        var ok = await c.PostAsync("/api/auth/me/avatar", WebpForm("file"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var key = (await Json(ok)).GetProperty("AvatarKey").GetString();
        Assert.StartsWith($"avatars/{u.Id}/", key);
        Assert.Equal(key, (await Json(await c.GetAsync("/api/auth/me"))).GetProperty("AvatarKey").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/auth/me/avatar", WebpForm("file", "a.png", "image/png"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/auth/me/avatar", WebpForm("file", "a.webp", "image/webp", "not a webp at all"u8.ToArray()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/auth/me/avatar", WebpForm("file", "a.webp", "image/jpeg"))).StatusCode);
        // avatar cũ không bị mất khi upload lỗi
        Assert.Equal(key, (await Json(await c.GetAsync("/api/auth/me"))).GetProperty("AvatarKey").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().PostAsync("/api/auth/me/avatar", WebpForm("file"))).StatusCode);
    }

    [LocalDbFact]
    public async Task UpdateProfile_ChangeApprovedCccd_BringsDocumentBackToPending_OverHttp()
    {
        var u = await _fx.SeedUser(UserRole.Customer, cccd: "012345678901");
        var c = await _fx.LoginAs(u.Email);
        var docId = await Submit(c, 1, "012345678901");
        var staff = await _fx.As(UserRole.Staff);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/admin/documents/{docId}/approve", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync("/api/auth/me", new { FullName = "Tên mới", IdentityNumber = "999988887777", Address = "HCM" })).StatusCode);

        var docs = await Json(await c.GetAsync("/api/me/documents"));
        Assert.Equal((int)DocumentStatus.ChoDuyet, StatusInt(docs[0].GetProperty("Status")));
    }

    private static int StatusInt(JsonElement e) => e.ValueKind == JsonValueKind.Number ? e.GetInt32() : (int)Enum.Parse<DocumentStatus>(e.GetString()!);

    // ---------- Giấy tờ ----------

    [LocalDbFact]
    public async Task Documents_Routes_RoleMatrix()
    {
        var anon = _fx.Anonymous();
        var customer = await _fx.As(UserRole.Customer);
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/me/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/me/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsync("/api/me/documents", DocForm(1, Digits(12)))).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/admin/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/admin/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/admin/documents")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/documents?status=1&page=1")).StatusCode);

        foreach (var url in new[] { "/api/admin/documents/99999999/approve", "/api/admin/documents/99999999/reject" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync(url, new { Reason = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync(url, new { Reason = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await staff.PostAsJsonAsync(url, new { Reason = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync(url, new { Reason = "x" })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/documents/1/file")).StatusCode);
    }

    [LocalDbFact]
    public async Task Documents_Submit_Validation_400()
    {
        var c = await _fx.As(UserRole.Customer);
        async Task<string?> Err(MultipartFormDataContent f) { var r = await c.PostAsync("/api/me/documents", f); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode); return await Code(r); }

        Assert.Equal("INVALID_DOCUMENT_NUMBER", await Err(DocForm(1, "12345")));
        Assert.Equal("INVALID_EXPIRY", await Err(DocForm(2, Digits(12))));
        Assert.Equal("INVALID_EXPIRY", await Err(DocForm(2, Digits(12), DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd"))));
        await Err(DocForm(7, Digits(12)));
        await Err(DocForm(1, Digits(12), frontName: "f.png")); // không phải WebP
        await Err(DocForm(1, new string('1', 31)));             // MaxLength 30
        // thiếu ảnh mặt trước
        var noFile = new MultipartFormDataContent { { new StringContent("1"), "Type" }, { new StringContent(Digits(12)), "Number" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/me/documents", noFile)).StatusCode);
    }

    [LocalDbFact]
    public async Task Documents_FullFlow_Submit_Resubmit_Approve_Reject_SoftDelete_And_CccdSync()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var c = await _fx.LoginAs(u.Email);
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);

        var number = Digits(12);
        var first = await Submit(c, 1, number);
        var second = await Submit(c, 1, Digits(12));   // nộp lại
        Assert.NotEqual(first, second);

        await using (var db = _fx.NewContext())
        {
            Assert.True((await db.Set<CustomerDocument>().SingleAsync(d => d.Id == first)).IsDeleted);
            Assert.Equal(1, await db.Set<CustomerDocument>().CountAsync(d => d.CustomerId == u.Id && d.Type == DocumentType.Cccd && !d.IsDeleted));
        }
        var mine = await Json(await c.GetAsync("/api/me/documents"));
        Assert.Equal(1, mine.GetArrayLength());
        Assert.DoesNotContain("private/", mine.GetRawText());   // không lộ key ảnh

        // Khách tự duyệt: 403 theo vai trò. Từ chối không lý do: 400.
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync($"/api/admin/documents/{second}/approve", null)).StatusCode);
        var noReason = await staff.PostAsJsonAsync($"/api/admin/documents/{second}/reject", new { Reason = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal("REASON_REQUIRED", await Code(noReason));
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync($"/api/admin/documents/{second}/reject", new { Reason = new string('x', 501) })).StatusCode); // L3

        var rej = await staff.PostAsJsonAsync($"/api/admin/documents/{second}/reject", new { Reason = "ảnh mờ" });
        Assert.Equal(HttpStatusCode.OK, rej.StatusCode);
        Assert.Equal("ảnh mờ", (await Json(rej)).GetProperty("RejectReason").GetString());
        // đã xử lý: duyệt tiếp là 409
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/admin/documents/{second}/approve", null)).StatusCode);

        // nộp lại sau từ chối rồi duyệt: đồng bộ CCCD sang hồ sơ khách
        var cccdNumber = Digits(12);
        var third = await Submit(c, 1, cccdNumber);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/documents/{third}/approve", null)).StatusCode);
        Assert.Equal(cccdNumber, (await Json(await c.GetAsync("/api/auth/me"))).GetProperty("IdentityNumber").GetString());

        // hàng đợi lọc theo trạng thái
        var queue = await Json(await staff.GetAsync("/api/admin/documents?status=1&pageSize=100"));
        Assert.DoesNotContain(queue.GetProperty("items").EnumerateArray(), i => i.GetProperty("Id").GetInt32() == third);
    }

    [LocalDbFact]
    public async Task Documents_SelfReview_Staff() => await SelfReview(UserRole.Staff);

    [LocalDbFact]
    public async Task Documents_SelfReview_Admin() => await SelfReview(UserRole.Admin);

    private async Task SelfReview(UserRole role)
    {
        var u = await _fx.SeedUser(role);
        var c = await _fx.LoginAs(u.Email);
        var id = await Submit(c, 1);
        var a = await c.PostAsync($"/api/admin/documents/{id}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, a.StatusCode);
        Assert.Equal("SELF_REVIEW", await Code(a));
        var r = await c.PostAsJsonAsync($"/api/admin/documents/{id}/reject", new { Reason = "x" });
        Assert.Equal("SELF_REVIEW", await Code(r));
        // người khác duyệt được
        Assert.Equal(HttpStatusCode.OK, (await (await _fx.As(UserRole.Admin)).PostAsync($"/api/admin/documents/{id}/approve", null)).StatusCode);
    }

    [LocalDbFact]
    public async Task DocumentFile_Access_OwnerStaffAdmin_OtherCustomerForbidden_Headers()
    {
        var owner = await _fx.SeedUser(UserRole.Customer);
        var oc = await _fx.LoginAs(owner.Email);
        var id = await Submit(oc, 1);
        var other = await _fx.As(UserRole.Customer);

        var r = await oc.GetAsync($"/api/documents/{id}/file?side=front");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("image/webp", r.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", r.Headers.CacheControl?.ToString() ?? string.Join(",", r.Headers.GetValues("Cache-Control")));
        Assert.Contains("private", string.Join(",", r.Headers.GetValues("Cache-Control")));
        Assert.True((await r.Content.ReadAsByteArrayAsync()).Length > 0);

        Assert.Equal(HttpStatusCode.OK, (await oc.GetAsync($"/api/documents/{id}/file?side=back")).StatusCode);   // Submit() gửi mặt sau
        Assert.Equal(HttpStatusCode.OK, (await (await _fx.As(UserRole.Staff)).GetAsync($"/api/documents/{id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await _fx.As(UserRole.Admin)).GetAsync($"/api/documents/{id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/documents/{id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/documents/{id}/file?side=back")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await oc.GetAsync($"/api/documents/{id}/file?side=top")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await oc.GetAsync("/api/documents/99999999/file")).StatusCode);
    }

    [LocalDbFact]
    public async Task DocumentFile_NoBackSide_404()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var c = await _fx.LoginAs(u.Email);
        var r = await c.PostAsync("/api/me/documents", DocForm(1, Digits(12), back: false));
        var j = await Json(r);
        var id = j.GetProperty("Id").GetInt32();
        Assert.False(j.GetProperty("HasBack").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/documents/{id}/file?side=back")).StatusCode);
    }

    // ---------- Điểm thưởng ----------

    [LocalDbFact]
    public async Task Loyalty_Routes_RoleMatrix_And_AdjustFlow()
    {
        var anon = _fx.Anonymous();
        var u = await _fx.SeedUser(UserRole.Customer);
        var c = await _fx.LoginAs(u.Email);
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/me/loyalty")).StatusCode);
        var me = await Json(await c.GetAsync("/api/me/loyalty"));
        Assert.Equal(0, me.GetProperty("Points").GetInt32());

        var url = $"/api/admin/customers/{u.Id}/loyalty/adjust";
        var body = new { Points = 500, Note = "thưởng" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync(url, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(url, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync(url, body)).StatusCode);

        var ok = await admin.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(500, (await Json(ok)).GetProperty("Points").GetInt32());
        Assert.Equal(1, (await Json(await c.GetAsync("/api/me/loyalty"))).GetProperty("History").GetArrayLength());

        var noNote = await admin.PostAsJsonAsync(url, new { Points = 5, Note = "" });
        Assert.Equal(HttpStatusCode.BadRequest, noNote.StatusCode);
        Assert.Equal("REASON_REQUIRED", await Code(noNote));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(url, new { Points = 5, Note = new string('n', 501) })).StatusCode); // L3
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(url, new { Points = 0, Note = "x" })).StatusCode);
        var neg = await admin.PostAsJsonAsync(url, new { Points = -501, Note = "x" });
        Assert.Equal("INSUFFICIENT_POINTS", await Code(neg));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/api/admin/customers/99999999/loyalty/adjust", body)).StatusCode);
    }

    [LocalDbFact]
    public async Task Return_AwardsExactly21000Once_TierAndHistoryUpdated_SecondReturnConflict()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var car = await _fx.SeedCar(CarStatus.DangThue);
        var bk = await SeedBooking(u.Id, car, BookingStatus.DangThue, -3, 0);
        var staff = await _fx.As(UserRole.Staff);
        await SeedGiaoRecord(bk);
        await GiveHandover(staff, bk, HandoverType.Nhan);

        var ret = await staff.PostAsJsonAsync($"/api/bookings/{bk}/return", new { ReturnedAt = DateTime.UtcNow.Date, DamageFee = 0 });
        Assert.Equal(HttpStatusCode.OK, ret.StatusCode);
        Assert.Equal(2_100_000m, (await Json(ret)).GetProperty("FinalAmount").GetDecimal());

        var again = await staff.PostAsJsonAsync($"/api/bookings/{bk}/return", new { ReturnedAt = DateTime.UtcNow.Date, DamageFee = 0 });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        var loy = await Json(await (await _fx.LoginAs(u.Email)).GetAsync("/api/me/loyalty"));
        Assert.Equal(21_000, loy.GetProperty("Points").GetInt32());
        Assert.Equal(21_000, loy.GetProperty("LifetimePoints").GetInt32());
        Assert.Equal(1, loy.GetProperty("History").GetArrayLength());
        Assert.Equal(79_000, loy.GetProperty("PointsToNextTier").GetInt32());

        await using var db = _fx.NewContext();
        Assert.Equal(1, await db.Set<LoyaltyTransaction>().CountAsync(t => t.BookingId == bk));
    }

    [LocalDbFact]
    public async Task Return_UsesFinalAmountIncludingLateAndDamage()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var car = await _fx.SeedCar(CarStatus.DangThue);
        var bk = await SeedBooking(u.Id, car, BookingStatus.DangThue, -3, -1);    // hết hạn hôm qua, trả hôm nay: trễ 1 ngày (700k) + hư hại 300k
        var staff = await _fx.As(UserRole.Staff);
        await SeedGiaoRecord(bk);
        await GiveHandover(staff, bk, HandoverType.Nhan);
        var ret = await staff.PostAsJsonAsync($"/api/bookings/{bk}/return", new { ReturnedAt = DateTime.UtcNow.Date, DamageFee = 300_000 });
        var final = (await Json(ret)).GetProperty("FinalAmount").GetDecimal();
        Assert.Equal(2_100_000m + 700_000m + 300_000m, final);
        await using var db = _fx.NewContext();
        Assert.Equal((int)(final * 0.01m), (await db.Customers.SingleAsync(x => x.Id == u.Id)).LoyaltyPoints);
    }

    // ---------- Chặn giao xe ----------

    [LocalDbFact]
    public async Task Pickup_Blocked_WhenDocumentsMissingPendingRejectedOrLicenseExpiresBeforeEnd_ThenAllowed()
    {
        var staff = await _fx.As(UserRole.Staff);
        async Task<(int Booking, int Customer)> Setup(Func<int, Task> docs, int endOffset = 3)
        {
            var u = await _fx.SeedUser(UserRole.Customer);
            await docs(u.Id);
            var car = await _fx.SeedCar();
            return (await SeedBooking(u.Id, car, BookingStatus.DaXacNhan, 0, endOffset), u.Id);
        }
        async Task AssertBlocked((int Booking, int Customer) s)
        {
            var r = await staff.PostAsync($"/api/bookings/{s.Booking}/pickup", null);
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            Assert.Equal("DOCUMENTS_NOT_VERIFIED", await Code(r));
            await using var db = _fx.NewContext();
            Assert.Equal(BookingStatus.DaXacNhan, (await db.Bookings.SingleAsync(b => b.Id == s.Booking)).Status);
        }

        await AssertBlocked(await Setup(_ => Task.CompletedTask));                                                     // chưa nộp gì
        await AssertBlocked(await Setup(id => SeedApprovedDocs(id, withLicense: false)));                              // thiếu bằng lái
        await AssertBlocked(await Setup(id => SeedApprovedDocs(id, withCccd: false)));                                 // thiếu CCCD
        await AssertBlocked(await Setup(id => SeedApprovedDocs(id, licenseStatus: DocumentStatus.ChoDuyet)));          // bằng lái chờ duyệt
        await AssertBlocked(await Setup(id => SeedApprovedDocs(id, cccdStatus: DocumentStatus.TuChoi)));               // CCCD bị từ chối
        await AssertBlocked(await Setup(id => SeedApprovedDocs(id, licenseExpiry: DateTime.UtcNow.Date.AddDays(2))));  // bằng lái hết hạn trước EndDate (+3)

        var ok = await Setup(id => SeedApprovedDocs(id, licenseExpiry: DateTime.UtcNow.Date.AddDays(3)));             // hết hạn đúng ngày trả: được
        await GiveHandover(staff, ok.Booking, HandoverType.Giao);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/bookings/{ok.Booking}/pickup", null)).StatusCode);
    }

    [LocalDbFact]
    public async Task Booking_Create_NotBlockedByMissingDocuments()
    {
        var u = await _fx.SeedUser(UserRole.Customer);
        var car = await _fx.SeedCar();
        var r = await (await _fx.LoginAs(u.Email)).PostAsJsonAsync("/api/bookings", new
        {
            CarId = car, StartDate = DateTime.UtcNow.Date.AddDays(5), EndDate = DateTime.UtcNow.Date.AddDays(7)
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [LocalDbFact]
    public async Task Pickup_Authorization_CustomerForbidden_Anonymous401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fx.Anonymous().PostAsync("/api/bookings/1/pickup", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _fx.As(UserRole.Customer)).PostAsync("/api/bookings/1/pickup", null)).StatusCode);
    }

    // ---------- L4: xe ChoDuyet không công khai với khách ----------

    [LocalDbFact]
    public async Task L4_PendingCar_EstimateAndBookedRanges_404ForCustomer_OkForStaffAndAdmin()
    {
        var car = await _fx.SeedCar(CarStatus.ChoDuyet);
        var est = new { CarId = car, StartDate = DateTime.UtcNow.Date.AddDays(5), EndDate = DateTime.UtcNow.Date.AddDays(7) };
        var customer = await _fx.As(UserRole.Customer);

        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync($"/api/bookings/car/{car}/booked-ranges")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.PostAsJsonAsync("/api/bookings/estimate", est)).StatusCode);
        foreach (var role in new[] { UserRole.Staff, UserRole.Admin })
        {
            var c = await _fx.As(role);
            Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/bookings/car/{car}/booked-ranges")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/bookings/estimate", est)).StatusCode);
        }
        var approved = await _fx.SeedCar();
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync($"/api/bookings/car/{approved}/booked-ranges")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsJsonAsync("/api/bookings/estimate", new { CarId = approved, est.StartDate, est.EndDate })).StatusCode);
    }

    // ---------- Quản lý khách ----------

    [LocalDbFact]
    public async Task AdminCustomers_RoleMatrix_ListOnlyCustomers_StaffRedactedAdminFull()
    {
        var anon = _fx.Anonymous();
        var customer = await _fx.As(UserRole.Customer);
        var staff = await _fx.As(UserRole.Staff);
        var admin = await _fx.As(UserRole.Admin);
        var cccd = Digits(12);
        var target = await _fx.SeedUser(UserRole.Customer, cccd: cccd);
        await using (var db = _fx.NewContext())
        {
            var t = await db.Customers.SingleAsync(x => x.Id == target.Id);
            t.Address = "123 Lê Lợi";
            t.IsLocked = true; t.LockReason = "gian lận"; t.LockedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        await (await _fx.As(UserRole.Admin)).PostAsJsonAsync($"/api/admin/customers/{target.Id}/loyalty/adjust", new { Points = 100, Note = "thử" });

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/admin/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/admin/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync($"/api/admin/customers/{target.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync($"/api/admin/customers/{target.Id}")).StatusCode);

        var list = await Json(await staff.GetAsync("/api/admin/customers?pageSize=100"));
        var items = list.GetProperty("items").EnumerateArray().ToList();
        Assert.All(items, i => Assert.DoesNotContain(i.GetProperty("Email").GetString(), new[] { "Staff", "Admin" }));
        await using (var db = _fx.NewContext())
        {
            var staffEmails = await db.Customers.Where(c => c.Role != UserRole.Customer).Select(c => c.Email).ToListAsync();
            Assert.DoesNotContain(items, i => staffEmails.Contains(i.GetProperty("Email").GetString()!));
        }
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/customers?tier=1&docStatus=1&blacklisted=false")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/admin/customers?search={Uri.EscapeDataString(target.Email)}")).StatusCode);

        var sj = await Json(await staff.GetAsync($"/api/admin/customers/{target.Id}"));
        Assert.Equal(JsonValueKind.Null, sj.GetProperty("IdentityNumber").ValueKind);
        Assert.Equal(JsonValueKind.Null, sj.GetProperty("Address").ValueKind);
        Assert.Equal(JsonValueKind.Null, sj.GetProperty("LockReason").ValueKind);
        Assert.Equal(0, sj.GetProperty("Loyalty").GetProperty("History").GetArrayLength());
        Assert.True(sj.GetProperty("IsLocked").GetBoolean());

        var aj = await Json(await admin.GetAsync($"/api/admin/customers/{target.Id}"));
        Assert.Equal(cccd, aj.GetProperty("IdentityNumber").GetString());
        Assert.Equal("123 Lê Lợi", aj.GetProperty("Address").GetString());
        Assert.Equal("gian lận", aj.GetProperty("LockReason").GetString());
        Assert.Equal(1, aj.GetProperty("Loyalty").GetProperty("History").GetArrayLength());
        Assert.DoesNotContain("PasswordHash", aj.GetRawText());

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/customers/99999999")).StatusCode);
    }

    // ---------- PHẦN 0 qua HTTP: L2, L3, M1 ----------

    [LocalDbFact]
    public async Task L2_RemoveBlacklist_RequiresReason_AndL3_MaxLength400()
    {
        var admin = await _fx.As(UserRole.Admin);
        var created = await admin.PostAsJsonAsync("/api/admin/blacklist", new { IdentityNumber = Digits(12), Level = 2, Reason = "r" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await Json(created)).GetProperty("Id").GetInt32();

        var noReason = await admin.PostAsJsonAsync($"/api/admin/blacklist/{id}/remove", new { Reason = " " });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal("REASON_REQUIRED", await Code(noReason));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/admin/blacklist/{id}/remove", new { Reason = new string('x', 501) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/admin/blacklist/{id}/remove", new { Reason = "đã giải quyết" })).StatusCode);

        // L3 trên tạo mới
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/blacklist", new { IdentityNumber = Digits(12), Level = 2, Reason = new string('x', 501) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/blacklist", new { IdentityNumber = new string('1', 21), Level = 2, Reason = "r" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/admin/blacklist", new { PhoneNumber = new string('1', 21), Level = 2, Reason = "r" })).StatusCode);
        // ranh giới: đúng 500 ký tự vẫn được
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/blacklist", new { IdentityNumber = Digits(12), Level = 2, Reason = new string('x', 500) })).StatusCode);
    }

    [LocalDbFact]
    public async Task M1_BlacklistByCustomerId_OverHttp_BlocksSameCccdOnNewAccount()
    {
        var cccd = Digits(12);
        var victim = await _fx.SeedUser(UserRole.Customer, cccd: cccd);
        var admin = await _fx.As(UserRole.Admin);
        var r = await admin.PostAsJsonAsync("/api/admin/blacklist", new { CustomerId = victim.Id, Level = 2, Reason = "gian lận" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.Equal(cccd, (await Json(r)).GetProperty("IdentityNumber").GetString());

        var fresh = await _fx.SeedUser(UserRole.Customer, cccd: cccd);
        var car = await _fx.SeedCar();
        var book = await (await _fx.LoginAs(fresh.Email)).PostAsJsonAsync("/api/bookings", new
        {
            CarId = car, StartDate = DateTime.UtcNow.Date.AddDays(5), EndDate = DateTime.UtcNow.Date.AddDays(7)
        });
        Assert.Equal("CUSTOMER_BLOCKED", await Code(book));
    }
}
