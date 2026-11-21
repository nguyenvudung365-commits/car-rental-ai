using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>Ảnh của biên bản bàn giao, lưu ở prefix private/ (chỉ đọc qua API có kiểm quyền).</summary>
public class HandoverPhoto : BaseEntity
{
    public int HandoverRecordId { get; set; }

    public HandoverRecord? HandoverRecord { get; set; }

    public string ObjectKey { get; set; } = string.Empty;
}
