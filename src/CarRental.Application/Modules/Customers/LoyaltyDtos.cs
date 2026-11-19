using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Customers;

public record LoyaltyTransactionDto(int Id, int? BookingId, int Points, LoyaltyReason Reason, string? Note, DateTime CreatedAt);

public record LoyaltyDto(
    int Points,
    int LifetimePoints,
    MemberTier Tier,
    MemberTier? NextTier,
    int PointsToNextTier,
    List<LoyaltyTransactionDto> History);

public record AdjustLoyaltyRequest(
    [Range(-10_000_000, 10_000_000)] int Points,
    [MaxLength(500)] string? Note);
