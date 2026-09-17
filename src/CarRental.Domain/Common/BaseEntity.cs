namespace CarRental.Domain.Common;

/// <summary>
/// Base class cho mọi entity. Chứa khoá chính và dấu vết thời gian.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft delete — không xoá vật lý để giữ lịch sử booking.</summary>
    public bool IsDeleted { get; set; }
}
