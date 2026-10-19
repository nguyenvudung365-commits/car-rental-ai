using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>Bảng Payment — một booking có thể có nhiều bản ghi thanh toán.</summary>
public class Payment : BaseEntity
{
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.ChuaThanhToan;

    /// <summary>Mã giao dịch từ cổng thanh toán / nhân viên ghi nhận.</summary>
    public string? TransactionCode { get; set; }

    public DateTime? PaidAt { get; set; }
}
