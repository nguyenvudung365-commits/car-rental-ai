using CarRental.Application.Modules.Bookings;
using CarRental.Infrastructure.Pdf;
using CarRental.Infrastructure.Persistence.Repository;

namespace CarRental.API.DependencyInjection;

public static class BookingsRegistration
{
    /// <summary>Đặt xe/thuê xe: service thật được bọc bởi decorator Guard (luật chặn đặt ở Guard, không sửa service gốc).</summary>
    public static IServiceCollection AddBookingsModule(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<BookingService>();
        services.AddScoped<IBookingService>(sp =>
            ActivatorUtilities.CreateInstance<BookingServiceGuard>(sp, sp.GetRequiredService<BookingService>()));
        services.AddSingleton<IContractPdfGenerator, ContractPdfGenerator>();
        services.AddScoped<RentalService>();
        services.AddScoped<IRentalService>(sp =>
            ActivatorUtilities.CreateInstance<RentalServiceGuard>(sp, sp.GetRequiredService<RentalService>()));
        return services;
    }
}
