using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Customers;

/// <summary>
/// Giấy tờ xác minh (CCCD/bằng lái). Mỗi khách có tối đa một bản ghi hiệu lực cho mỗi loại;
/// nộp lại thì bản cũ bị soft-delete. Ảnh nằm ở prefix private/, chỉ đọc qua API có kiểm quyền.
/// </summary>
public class CustomerDocument : BaseEntity
{
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DocumentType Type { get; set; }

    public string Number { get; set; } = string.Empty;

    public string FrontKey { get; set; } = string.Empty;

    public string? BackKey { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? LicenseClass { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.ChoDuyet;

    public string? RejectReason { get; set; }

    public int? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
