using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>Bảng ReturnRecord — biên bản trả xe, phạt trễ hạn / hư hỏng.</summary>
public class ReturnRecord : BaseEntity
{
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public DateTime ReturnedAt { get; set; }

    /// <summary>Số ngày trả trễ (0 nếu đúng hạn).</summary>
    public int LateDays { get; set; }

    public decimal LateFee { get; set; }

    public decimal DamageFee { get; set; }

    public string? DamageNote { get; set; }

    /// <summary>Tổng tiền chốt sau khi trả xe = TotalPrice + LateFee + DamageFee.</summary>
    public decimal FinalAmount { get; set; }

    /// <summary>Nhân viên lập biên bản.</summary>
    public int? StaffId { get; set; }
}
