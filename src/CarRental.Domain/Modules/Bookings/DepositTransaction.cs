using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>Giao dịch tiền cọc bảo đảm. Cọc không phải doanh thu và không nằm trong bảng Payments (trừ khoản KhauTru sinh ra một Payment TruCoc).</summary>
public class DepositTransaction : BaseEntity
{
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public DepositTxType Type { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public string? Note { get; set; }

    public int? StaffId { get; set; }
}
