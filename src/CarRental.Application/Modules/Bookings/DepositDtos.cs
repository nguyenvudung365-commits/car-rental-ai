using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Bookings;

public record CollectDepositRequest([Required] PaymentMethod Method);

/// <summary>RefundMethod dùng cho phần cọc còn lại hoàn cho khách (không nhận TruCoc).</summary>
public record SettleDepositRequest([Required] PaymentMethod RefundMethod, [StringLength(500)] string? Note);

public record DepositTransactionDto(int Id, string Type, decimal Amount, string Method, string? Note, DateTime CreatedAt);

/// <summary>Status: ChuaThu, DangGiu, DaQuyetToan. Held = đang giữ (đã thu trừ đã khấu trừ và đã hoàn).</summary>
public record DepositSummaryDto(
    decimal Required,
    decimal Collected,
    decimal Held,
    decimal Deducted,
    decimal Refunded,
    string Status,
    List<DepositTransactionDto> Items);
