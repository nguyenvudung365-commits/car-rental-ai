using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>Biên bản bàn giao xe (giao hoặc nhận lại): số km, mức xăng, tình trạng, ảnh. Mỗi đơn tối đa một biên bản mỗi loại.</summary>
public class HandoverRecord : BaseEntity
{
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public HandoverType Type { get; set; }

    public int Odometer { get; set; }

    /// <summary>Mức nhiên liệu 0-100 %.</summary>
    public int FuelLevel { get; set; }

    public string? ConditionNote { get; set; }

    public int? StaffId { get; set; }

    public ICollection<HandoverPhoto> Photos { get; set; } = new List<HandoverPhoto>();
}
