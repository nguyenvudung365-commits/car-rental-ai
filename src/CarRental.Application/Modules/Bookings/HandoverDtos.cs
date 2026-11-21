using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Bookings;

public record CreateHandoverRequest(
    [Required] HandoverType Type,
    [Range(0, 2_000_000)] int Odometer,
    [Range(0, 100)] int FuelLevel,
    [StringLength(1000)] string? ConditionNote);

/// <summary>Không trả object key: ảnh lấy qua GET api/handover-photos/{id}.</summary>
public record HandoverPhotoDto(int Id);

public record HandoverDto(
    int Id,
    int BookingId,
    HandoverType Type,
    int Odometer,
    int FuelLevel,
    string? ConditionNote,
    int? StaffId,
    DateTime CreatedAt,
    List<HandoverPhotoDto> Photos);

public record HandoverPhotoFile(Stream Content, string ContentType);
