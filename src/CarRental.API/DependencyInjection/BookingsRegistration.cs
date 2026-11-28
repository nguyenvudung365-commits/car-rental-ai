using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Pricing;
using CarRental.Infrastructure.Pdf;
using CarRental.Infrastructure.Persistence.Repository;

namespace CarRental.API.DependencyInjection;

public static class BookingsRegistration
{
    /// <summary>Đặt xe/thuê xe: service thật được bọc bởi decorator Guard (luật chặn đặt ở Guard, không sửa service gốc).</summary>
    public static IServiceCollection AddBookingsModule(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Đợt 4: phí/mã giảm giá/chi nhánh. Phí cố định lấy từ section "Pricing" (mặc định trong PricingOptions).
        services.AddSingleton(configuration?.GetSection("Pricing").Get<PricingOptions>() ?? new PricingOptions());
        services.AddScoped<IPricingRepository, PricingRepository>();
        services.AddScoped<BookingChargeCalculator>();
        services.AddScoped<IBookingChargeCalculator>(sp => sp.GetRequiredService<BookingChargeCalculator>());
        services.AddScoped<PricingAdminService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<BookingService>();
        services.AddScoped<IBookingService>(sp =>
            ActivatorUtilities.CreateInstance<BookingServiceGuard>(sp, sp.GetRequiredService<BookingService>()));
        services.AddSingleton<IContractPdfGenerator, ContractPdfGenerator>();
        services.AddScoped<RentalService>();
        services.AddScoped<IRentalService>(sp =>
            ActivatorUtilities.CreateInstance<RentalServiceGuard>(sp, sp.GetRequiredService<RentalService>()));
        // Đợt 3: biên bản bàn giao, cọc bảo đảm, gia hạn. DepositService dùng RentalService gốc để Guard không tạo vòng phụ thuộc.
        services.AddScoped<IHandoverRepository, HandoverRepository>();
        services.AddScoped<IHandoverService, HandoverService>();
        services.AddScoped<IDepositRepository, DepositRepository>();
        services.AddScoped<IDepositService>(sp =>
            ActivatorUtilities.CreateInstance<DepositService>(sp, sp.GetRequiredService<RentalService>()));
        services.AddScoped<IBookingExtensionRepository, BookingExtensionRepository>();
        services.AddScoped<IBookingExtensionService, BookingExtensionService>();
        return services;
    }
}
