using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Customers;

/// <summary>
/// Danh sách chặn khách. Đang hiệu lực khi RemovedAt == null.
/// Khớp theo CustomerId hoặc CCCD hoặc SĐT để khách tạo tài khoản mới vẫn bị bắt.
/// </summary>
public class BlacklistEntry : BaseEntity
{
    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public string? IdentityNumber { get; set; }

    public string? PhoneNumber { get; set; }

    public BlacklistLevel Level { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int CreatedById { get; set; }

    public DateTime? RemovedAt { get; set; }

    public int? RemovedById { get; set; }

    public string? RemoveReason { get; set; }
}
