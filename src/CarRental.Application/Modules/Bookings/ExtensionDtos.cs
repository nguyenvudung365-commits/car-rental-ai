using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Bookings;

public record ExtendBookingRequest([Required] DateTime NewEndDate);

public record RejectExtensionRequest([StringLength(500)] string? Reason);

/// <summary>Báo giá gia hạn (chỉ kiểm tra): Available = false khi xe đã có lịch khác trong khoảng gia hạn.</summary>
public record ExtensionQuoteDto(DateTime OldEndDate, DateTime NewEndDate, int ExtraDays, decimal ExtraAmount, decimal NewTotalPrice, bool Available);

public record ExtensionDto(
    int Id,
    int BookingId,
    DateTime OldEndDate,
    DateTime NewEndDate,
    int ExtraDays,
    decimal ExtraAmount,
    ExtensionStatus Status,
    string? RejectReason,
    int RequestedById,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? CustomerName,
    string? CarName);
