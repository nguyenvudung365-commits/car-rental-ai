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
    List<CarImageDto> Images);

public record CarImageDto(int Id, string FilePath, bool IsPrimary);

public record CreateCarRequest(
    CarType CarType,
    [Required, MinLength(1)] string Brand,
    [Required, MinLength(1)] string Model,
    [Required] string LicensePlate,
    [Range(1, 16)] int Seats,
    [Range(0, 50)] int CarAgeYears,
    [Range(1, double.MaxValue)] decimal BasePricePerDay,
    string? Description);

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
    string? Description);

public record CarFilterRequest(
    CarType? CarType,
    decimal? MinPrice,
    decimal? MaxPrice,
    CarStatus? Status,
    int Page = 1,
    int PageSize = 10);