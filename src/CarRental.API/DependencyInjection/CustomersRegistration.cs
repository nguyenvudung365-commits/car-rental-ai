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
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<DocumentService>();
        services.AddScoped<IDocumentService>(sp => sp.GetRequiredService<DocumentService>());
        services.AddScoped<IDocumentVerification>(sp => sp.GetRequiredService<DocumentService>());
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<ICustomerAdminRepository, CustomerAdminRepository>();
        services.AddScoped<ICustomerAdminService, CustomerAdminService>();
        return services;
    }
}
