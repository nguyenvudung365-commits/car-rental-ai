using CarRental.API.Middleware;
using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Customers;
using CarRental.Application.Modules.Reports;
using CarRental.Application.Modules.Bookings;
using CarRental.Infrastructure.Ai;
using CarRental.Infrastructure.Files;
using CarRental.Infrastructure.Pdf;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Persistence.Repository;
using CarRental.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection(MinioSettings.SectionName));
builder.Services.Configure<AiSettings>(
    builder.Configuration.GetSection(AiSettings.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Lỗi validate model trả 400 cùng định dạng { code, message } như ExceptionHandlingMiddleware
        options.InvalidModelStateResponseFactory = ctx =>
        {
            // Lỗi parse JSON (có thể chứa đường dẫn $.Field) thì trả câu chung, không lộ chi tiết nội bộ
            var errors = ctx.ModelState.Values.SelectMany(v => v.Errors).ToList();
            var isJsonError = ctx.ModelState.Any(kv => kv.Key.StartsWith("$")
                || kv.Value!.Errors.Any(e => e.Exception is not null
                    || e.ErrorMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase)));
            var message = isJsonError
                ? "Dữ liệu gửi lên không hợp lệ"
                : string.Join("; ", errors
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrEmpty(m)));
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { code = "validation_error", message });
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// --- Database ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// --- JWT settings + authentication ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

// Dừng khởi động nếu khóa JWT thiếu/ngắn/còn là giá trị mẫu (bỏ qua ở môi trường Testing)
if (!builder.Environment.IsEnvironment("Testing"))
{
    const string sampleJwtKey = "your-super-secret-key-at-least-32-characters";
    if (string.IsNullOrWhiteSpace(jwtSettings?.Key) || jwtSettings.Key.Length < 32 || jwtSettings.Key == sampleJwtKey
        || jwtSettings.Key.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Jwt:Key chưa được cấu hình an toàn: cần tối thiểu 32 ký tự và khác giá trị mẫu (đặt qua biến Jwt__Key / JWT_KEY).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
});
builder.Services.AddAuthorization();

// --- Dependency Injection: Auth / Customer ---
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// --- Dependency Injection: Car ---
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<ICarService, CarService>();

// --- Dependency Injection: AI Pricing ---
var aiSettings = builder.Configuration.GetSection(AiSettings.SectionName).Get<AiSettings>() ?? new AiSettings();
builder.Services.AddHttpClient<IAiPricingClient, AiPricingClient>(client =>
{
    client.BaseAddress = new Uri(aiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
});

// --- Dependency Injection: Booking ---
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddSingleton<IContractPdfGenerator, ContractPdfGenerator>();
builder.Services.AddScoped<IRentalService, RentalService>();

// --- Dependency Injection: Reports ---
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ReportService>();

// --- Unit of work ---
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Tài khoản Admin/Staff cho môi trường dev: chỉ tạo khi có Seed__<Role>Email và Seed__<Role>Password (đặt trong .env)
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var seeds = new[]
    {
        (Key: "Admin", Name: "Quản trị viên", Phone: "0900000000", Role: CarRental.Domain.Common.UserRole.Admin),
        (Key: "Staff", Name: "Nhân viên", Phone: "0900000010", Role: CarRental.Domain.Common.UserRole.Staff),
    };
    foreach (var s in seeds)
    {
        var email = app.Configuration[$"Seed:{s.Key}Email"]?.Trim().ToLowerInvariant();
        var password = app.Configuration[$"Seed:{s.Key}Password"];
        if (!string.IsNullOrEmpty(password) && (password.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) || password.Length < 8))
        {
            app.Logger.LogWarning("Bỏ qua seed tài khoản {Role}: mật khẩu là giá trị mẫu (CHANGE_ME) hoặc ngắn hơn 8 ký tự", s.Key);
            continue;
        }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || db.Customers.Any(c => c.Email == email || c.PhoneNumber == s.Phone))
            continue;
        db.Customers.Add(new CarRental.Domain.Modules.Customers.Customer
        {
            FullName = s.Name,
            Email = email,
            PhoneNumber = s.Phone,
            PasswordHash = hasher.Hash(password),
            Role = s.Role
        });
        db.SaveChanges();
    }
}

app.UseCors();

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
    {
        context.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString("N");
    }

    context.Response.Headers["X-Correlation-Id"] = context.Request.Headers["X-Correlation-Id"].ToString();
    await next();
});

app.UseStaticFiles(); // để client tải được ảnh xe qua /uploads/cars/...

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var health = () => Results.Ok(new { status = "ok", service = "CarRental.API" });
app.MapGet("/health", health);
app.MapGet("/api/health", health);

app.Run();

public partial class Program { } // để CustomWebApplicationFactory<Program> dùng được ở Module 7 (integration test)
