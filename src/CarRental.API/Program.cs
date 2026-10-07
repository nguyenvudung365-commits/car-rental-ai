using CarRental.API.Middleware;
using CarRental.Application.Common;
using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Customers;
using CarRental.Application.Modules.Bookings;
using CarRental.Infrastructure.Ai;
using CarRental.Infrastructure.Files;
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

// --- Unit of work ---
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
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
