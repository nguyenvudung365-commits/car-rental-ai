using CarRental.API.Security;
using CarRental.Application.Common;
using CarRental.Application.Modules.Customers;
using CarRental.Infrastructure.Persistence.Repository;

namespace CarRental.API.DependencyInjection;

public static class CustomersRegistration
{
    /// <summary>Quản lý tài khoản, danh sách chặn, giấy tờ, điểm thưởng và người dùng hiện tại.</summary>
    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IBlacklistRepository, BlacklistRepository>();
        services.AddScoped<BlacklistService>();
        services.AddScoped<IBlacklistService>(sp => sp.GetRequiredService<BlacklistService>());
        services.AddScoped<ICustomerScreening>(sp => sp.GetRequiredService<BlacklistService>());
        return services;
    }
}
