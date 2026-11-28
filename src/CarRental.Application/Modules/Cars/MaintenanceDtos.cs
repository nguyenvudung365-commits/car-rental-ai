using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Cars;

/// <summary>Cost chỉ có giá trị với Admin (null với Staff).</summary>
public record MaintenanceDto(
    int Id, int CarId, string CarName, string LicensePlate, MaintenanceType Type, DateTime DueDate, int? DueOdometer,
    DateTime? StartedAt, DateTime? CompletedAt, decimal? Cost, string? Note, MaintenanceStatus Status);

public record CreateMaintenanceRequest(
    [Required] int CarId,
    MaintenanceType Type,
    [Required] DateTime DueDate,
    [Range(0, 10_000_000)] int? DueOdometer,
    [MaxLength(500)] string? Note);

public record StartMaintenanceRequest(bool Force = false);

public record CompleteMaintenanceRequest(
    [Range(0, 1_000_000_000)] decimal? Cost,
    [MaxLength(500)] string? Note,
    DateTime? NextDueDate);

/// <summary>Overdue: quá hạn ngày; Upcoming: tới hạn trong withinDays ngày; ByOdometer: số km mới nhất đã gần/đạt mốc.</summary>
public record MaintenanceDueDto(List<MaintenanceDto> Overdue, List<MaintenanceDto> Upcoming, List<MaintenanceDto> ByOdometer);

public record MaintenanceListFilter(int? CarId, MaintenanceStatus? Status);
