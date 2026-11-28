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

    /// <summary>Hộp số (tùy chọn — xe cũ chưa có dữ liệu thì null).</summary>
    public Transmission? Transmission { get; set; }

    public FuelType? FuelType { get; set; }

    /// <summary>Năm sản xuất (hiển thị cho khách; AI vẫn dùng CarAgeYears).</summary>
    public int? ManufactureYear { get; set; }

    /// <summary>Giá niêm yết theo ngày, VNĐ. AI dự đoán trên nền giá này.</summary>
    public decimal BasePricePerDay { get; set; }

    public CarStatus Status { get; set; } = CarStatus.SanSang;

    /// <summary>Giá do nhân viên override — ưu tiên cao nhất trong công thức giá.</summary>
    public decimal? OverridePrice { get; set; }

    public string? Description { get; set; }

    /// <summary>Tiền cọc bảo đảm khi nhận xe (0 = không thu cọc). Chỉ Admin đổi.</summary>
    public decimal SecurityDeposit { get; set; }

    /// <summary>Chi nhánh xe đang đỗ (null = chưa gán). Cập nhật khi trả xe ở chi nhánh khác.</summary>
    public int? BranchId { get; set; }

    /// <summary>Giá thuê theo giờ (null = xe không cho thuê theo giờ).</summary>
    public decimal? PricePerHour { get; set; }

    public ICollection<CarImage> Images { get; set; } = new List<CarImage>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
