using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Bookings;

public record CreatePaymentRequest(
    [Range(typeof(decimal), "0.01", "1000000000")] decimal Amount,
    [Required] PaymentMethod Method,
    [StringLength(100)] string? TransactionCode
);

public record MarkPaymentPaidRequest(
    [Required] PaymentStatus Status,
    [StringLength(100)] string? TransactionCode
);

public record PaymentDto(
    int Id,
    int BookingId,
    decimal Amount,
    string Method,
    string Status,
    string? TransactionCode,
    DateTime? PaidAt,
    DateTime CreatedAt
);

/// <summary>Lịch sử thanh toán kèm tổng hợp. AmountToPay = giá thuê (hoặc FinalAmount sau khi trả xe); AmountDue = AmountToPay trừ các khoản đã ghi nhận.</summary>
public record BookingPaymentsDto(
    List<PaymentDto> Items,
    decimal TotalPrice,
    decimal AmountToPay,
    decimal PaidAmount,
    decimal AmountDue
);

public record ReturnBookingRequest(
    DateTime? ReturnedAt,
    [Range(typeof(decimal), "0", "1000000000")] decimal DamageFee,
    [StringLength(1000)] string? DamageNote
);

public record ReturnRecordDto(
    int BookingId,
    DateTime ReturnedAt,
    DateTime EndDate,
    int LateDays,
    decimal LateFee,
    decimal DamageFee,
    string? DamageNote,
    decimal TotalPrice,
    decimal FinalAmount,
    int? StaffId
);
