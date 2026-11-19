using CarRental.Application.Common;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Audit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarRental.Tests.Support;

/// <summary>DB LocalDB tạm cho test đợt 1 (tự migrate, tự xóa). Available=false nếu không có SQL Server.</summary>
public class TempDb : IAsyncLifetime
{
    public string Conn { get; private set; } = "";
    public bool Available { get; private set; }

    public virtual async Task InitializeAsync()
    {
        Conn = LocalDbFactAttribute.ServerConnection + ";Database=CarRentalTest_" + Guid.NewGuid().ToString("N");
        try
        {
            await using var db = NewContext();
            await db.Database.MigrateAsync();
            Available = true;
        }
        catch (Exception) { Available = false; }
    }

    public virtual async Task DisposeAsync()
    {
        if (!Available) return;
        await using var db = NewContext();
        await db.Database.EnsureDeletedAsync();
    }

    public ApplicationDbContext NewContext(ICurrentUser? actor = null)
    {
        var b = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Conn);
        if (actor is not null) b.AddInterceptors(new AuditSaveChangesInterceptor(actor));
        return new ApplicationDbContext(b.Options);
    }
}

public sealed class FakeActor : ICurrentUser
{
    public int? UserId { get; set; } = 99;
    public string? Role { get; set; } = "Admin";
    public string? IpAddress { get; set; } = "127.0.0.1";
    public bool IsAdmin => Role == "Admin";
    public bool IsStaffOrAdmin => IsAdmin || Role == "Staff";
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _conn;
    public ApiFactory(string conn) => _conn = conn;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // bỏ qua kiểm tra khóa JWT mẫu và không tự migrate
        builder.UseSetting("ConnectionStrings:Default", _conn);
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<IAiPricingClient>();
            s.AddScoped<IAiPricingClient, FakeAi>();
            s.RemoveAll<IFileStorageService>();
            s.AddScoped<IFileStorageService, FakeStorage>();
        });
    }

    private sealed class FakeAi : IAiPricingClient
    {
        public Task<PredictPriceResult> PredictPriceAsync(PredictPriceRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new PredictPriceResult(900_000, false));
    }

    /// <summary>Giữ cùng luật kiểm WebP như MinioFileStorageService; OpenRead trả ảnh WebP tối thiểu.</summary>
    private sealed class FakeStorage : IFileStorageService
    {
        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            await using var s = file.OpenReadStream();
            await CarRental.Infrastructure.Files.WebpValidator.ValidateAsync(s, file.FileName, file.ContentType, file.Length);
            return folder + "/" + Guid.NewGuid().ToString("N") + ".webp";
        }

        public void Delete(string relativePath) { }

    }
}

public static class WebpBytes
{
    /// <summary>12 byte đầu hợp lệ (RIFF....WEBP) cộng thêm vài byte đệm.</summary>
    public static byte[] Tiny => "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
}
