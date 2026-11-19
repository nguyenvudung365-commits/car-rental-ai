using CarRental.Application.Modules.Cars;
using CarRental.Application.Modules.Reports;
using CarRental.Infrastructure.Persistence.Repository;
using CarRental.Infrastructure.Reports;

namespace CarRental.API.DependencyInjection;

public static class FleetOpsRegistration
{
    /// <summary>Đợt 5: lịch bảo trì, lịch xe, dashboard, xuất báo cáo, nhập xe từ Excel.</summary>
    public static IServiceCollection AddFleetOpsModule(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<ICalendarQuery, CalendarQuery>();
        services.AddScoped<CalendarService>();
        services.AddScoped<IDashboardQuery, DashboardQuery>();
        services.AddSingleton<IReportExporter, ReportExporter>();
        services.AddScoped<ReportExportService>();
        services.AddSingleton<ICarSheetReader, CarSheetReader>();
        services.AddScoped<CarImportService>();
        return services;
    }
}
