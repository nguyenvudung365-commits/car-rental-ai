using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.Modules.Bookings;

public record BookingDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int CarId,
    string CarName,
    string LicensePlate,
    DateTime StartDate,
    DateTime EndDate,
    int RentalDays,
    decimal? PredictedPricePerDay,
    decimal FinalPricePerDay,
    decimal TotalPrice,
    bool IsFallback,
    string Status,
    string? CorrelationId,
    string? Note,
    DateTime CreatedAt
);

public record CreateBookingRequest(
    [Required] int CarId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    string? Note
);

public record BookingFilterRequest(
    int? CustomerId,
    int? CarId,
    string? Status,
    DateTime? StartDateFrom,
    DateTime? StartDateTo
)
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 10;
}

public record ConfirmBookingRequest;

public record CancelBookingRequest(
    string? Reason
);

public record BookedRangeDto(DateTime StartDate, DateTime EndDate);

public record EstimateBookingRequest(
    [Required] int CarId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);

public record BookingEstimateDto(
    int RentalDays,
    decimal BasePricePerDay,
    decimal? PredictedPricePerDay,
    decimal FinalPricePerDay,
    decimal TotalPrice,
    bool IsFallback
);
