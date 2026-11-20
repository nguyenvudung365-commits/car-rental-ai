using CarRental.Application.Modules.Audit;
using CarRental.Infrastructure.Persistence.Audit;

namespace CarRental.API.DependencyInjection;

public static class AuditRegistration
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();
        return services;
    }
}
