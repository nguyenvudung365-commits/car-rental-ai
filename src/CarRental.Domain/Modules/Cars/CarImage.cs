using CarRental.Domain.Common;

namespace CarRental.Domain.Modules.Cars;

/// <summary>Bảng CarImage — nhiều ảnh cho một xe, có ảnh đại diện.</summary>
public class CarImage : BaseEntity
{
    public int CarId { get; set; }

    public Car? Car { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Ảnh đại diện hiển thị ở danh sách xe.</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
