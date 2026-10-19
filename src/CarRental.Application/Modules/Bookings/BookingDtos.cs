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
    DateTime? StartDateTo,
    int Page = 1,
    int PageSize = 10
);

public record ConfirmBookingRequest;

public record CancelBookingRequest(
    string? Reason
);
