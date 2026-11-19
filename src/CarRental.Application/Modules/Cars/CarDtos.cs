using CarRental.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.Modules.Cars;

public record CarDto(
    int Id,
    CarType CarType,
    string Brand,
    string Model,
    string LicensePlate,
    int Seats,
    int CarAgeYears,
    decimal BasePricePerDay,
    CarStatus Status,
    decimal? OverridePrice,
    string? Description,
    List<CarImageDto> Images,
    Transmission? Transmission = null,
    FuelType? FuelType = null,
    int? ManufactureYear = null,
    decimal SecurityDeposit = 0,
    int? BranchId = null,
    decimal? PricePerHour = null);

public record CarImageDto(int Id, string FilePath, bool IsPrimary);

public record CreateCarRequest(
    CarType CarType,
    [Required, MinLength(1)] string Brand,
    [Required, MinLength(1)] string Model,
    [Required] string LicensePlate,
    [Range(1, 16)] int Seats,
    [Range(0, 50)] int CarAgeYears,
    [Range(1, double.MaxValue)] decimal BasePricePerDay,
    string? Description,
    Transmission? Transmission = null,
    FuelType? FuelType = null,
    [Range(1990, 2100)] int? ManufactureYear = null,
    [Range(0, 1_000_000_000)] decimal? SecurityDeposit = null,
    int? BranchId = null,
    [Range(1, 100_000_000)] decimal? PricePerHour = null);

public record UpdateCarRequest(
    CarType CarType,
    string Brand,
    string Model,
    string LicensePlate,
    int Seats,
    int CarAgeYears,
    decimal BasePricePerDay,
    CarStatus Status,
    decimal? OverridePrice,
    string? Description,
    Transmission? Transmission = null,
    FuelType? FuelType = null,
    [Range(1990, 2100)] int? ManufactureYear = null,
    [Range(0, 1_000_000_000)] decimal? SecurityDeposit = null, // null = giữ nguyên
    int? BranchId = null, // null = giữ nguyên
    [Range(1, 100_000_000)] decimal? PricePerHour = null); // null = giữ nguyên

/// <summary>Admin duyệt xe chờ duyệt; có thể kèm đặt/sửa giá (bỏ trống thì giữ giá Staff đề xuất).</summary>
public record ApproveCarRequest(decimal? BasePricePerDay = null, decimal? OverridePrice = null);

public record CarFilterRequest(
    CarType? CarType,
    decimal? MinPrice,
    decimal? MaxPrice,
    CarStatus? Status,
    int Page = 1,
    int PageSize = 10,
    DateTime? AvailableFrom = null,
    DateTime? AvailableTo = null,
    bool IncludePending = false,
    int? BranchId = null); // service tự đặt theo vai trò, giá trị từ query bị ghi đè