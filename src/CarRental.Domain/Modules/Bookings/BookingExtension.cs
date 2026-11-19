using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>Yêu cầu gia hạn thuê: khách gửi, Staff/Admin duyệt. Khi duyệt mới đổi EndDate và TotalPrice của đơn.</summary>
public class BookingExtension : BaseEntity
{
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public DateTime OldEndDate { get; set; }

    public DateTime NewEndDate { get; set; }

    public decimal ExtraAmount { get; set; }

    public int RequestedById { get; set; }

    public ExtensionStatus Status { get; set; } = ExtensionStatus.ChoDuyet;

    public int? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectReason { get; set; }
}
