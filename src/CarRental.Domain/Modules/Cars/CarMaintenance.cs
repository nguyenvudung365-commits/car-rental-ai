using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Cars;

/// <summary>Lịch bảo dưỡng, đăng kiểm, bảo hiểm của một xe. DueDate là ngày (UTC), nhắc hạn tính lúc đọc.</summary>
public class CarMaintenance : BaseEntity
{
    public int CarId { get; set; }

    public Car? Car { get; set; }

    public MaintenanceType Type { get; set; }

    public DateTime DueDate { get; set; }

    /// <summary>Mốc km cần làm (so với số km mới nhất ở biên bản bàn giao).</summary>
    public int? DueOdometer { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public decimal? Cost { get; set; }

    public string? Note { get; set; }

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.KeHoach;
}
