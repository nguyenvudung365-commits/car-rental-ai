using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Domain.Modules.Cars;

/// <summary>
/// Bảng Car — đầu xe cho thuê.
/// BasePricePerDay là giá niêm yết, cũng là giá fallback khi AI lỗi/quá 2 giây.
/// </summary>
public class Car : BaseEntity
{
    public CarType CarType { get; set; }

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    /// <summary>Biển số — duy nhất (lỗi 409 PLATE_EXISTS).</summary>
    public string LicensePlate { get; set; } = string.Empty;

    public int Seats { get; set; }

    public int CarAgeYears { get; set; }

    /// <summary>Giá niêm yết theo ngày, VNĐ. AI dự đoán trên nền giá này.</summary>
    public decimal BasePricePerDay { get; set; }

    public CarStatus Status { get; set; } = CarStatus.SanSang;

    /// <summary>Giá do nhân viên override — ưu tiên cao nhất trong công thức giá.</summary>
    public decimal? OverridePrice { get; set; }

    public string? Description { get; set; }

    public ICollection<CarImage> Images { get; set; } = new List<CarImage>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
