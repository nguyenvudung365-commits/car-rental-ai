using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Domain.Modules.Pricing;

/// <summary>Một dòng phí của đơn. Booking.TotalPrice = tiền thuê + tổng Amount các dòng (giảm giá là số âm).</summary>
public class BookingCharge : BaseEntity
{
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    public ChargeKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int? CouponId { get; set; }

    public int? SurchargeId { get; set; }
}
