namespace CarRental.Application.Modules.Cars;

public interface IMaintenanceService
{
    Task<List<MaintenanceDto>> ListAsync(MaintenanceListFilter filter, bool isAdmin);
    Task<MaintenanceDto> CreateAsync(CreateMaintenanceRequest request, bool isAdmin);
    Task<MaintenanceDueDto> GetDueAsync(int withinDays, bool isAdmin);
    Task<MaintenanceDto> StartAsync(int id, bool force, bool isAdmin);
    Task<MaintenanceDto> CompleteAsync(int id, CompleteMaintenanceRequest request, bool isAdmin);
    Task<MaintenanceDto> CancelAsync(int id, bool isAdmin);
}
