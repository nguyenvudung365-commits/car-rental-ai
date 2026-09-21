using CarRental.Domain.Common;
using CarRental.Domain.Modules.Cars;
using CarRental.Domain.Modules.Customers;

namespace CarRental.Domain.Modules.Bookings;

/// <summary>
/// Bảng Booking — đơn thuê xe.
/// Giá chốt theo: FinalPricePerDay = COALESCE(OverridePrice, PredictedPrice, BasePricePerDay), chặn [400.000đ, 1.500.000đ].
/// </summary>
public class Booking : BaseEntity
{
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int CarId { get; set; }

    public Car? Car { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Giá AI dự đoán tại thời điểm đặt (lưu lại để đối soát).</summary>
    public decimal? PredictedPricePerDay { get; set; }

    /// <summary>Giá chốt mỗi ngày sau COALESCE + clamp.</summary>
    public decimal FinalPricePerDay { get; set; }

    public decimal TotalPrice { get; set; }

    /// <summary>True khi AI lỗi/quá timeout và hệ thống dùng giá niêm yết.</summary>
    public bool IsFallback { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.ChoXacNhan;

    /// <summary>Để trace request giữa .NET và FastAPI.</summary>
    public string? CorrelationId { get; set; }

    public string? Note { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ReturnRecord? ReturnRecord { get; set; }
}
