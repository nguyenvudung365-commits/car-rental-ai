namespace CarRental.Domain.Modules.Audit;

/// <summary>Nhật ký thao tác — chỉ thêm, không sửa/xóa, nên không kế thừa BaseEntity.</summary>
public class AuditLog
{
    public long Id { get; set; }

    public int? ActorId { get; set; }

    public string? ActorRole { get; set; }

    /// <summary>Dạng "Car.Updated", "Booking.Updated.Status"...</summary>
    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    /// <summary>JSON { prop: [cũ, mới] }, tối đa 8 KB.</summary>
    public string? Changes { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
