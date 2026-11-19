using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Customers;

public record BlacklistEntryDto(
    int Id,
    int? CustomerId,
    string? CustomerName,
    string? IdentityNumber,
    string? PhoneNumber,
    BlacklistLevel Level,
    string Reason,
    int CreatedById,
    DateTime CreatedAt,
    DateTime? RemovedAt,
    int? RemovedById,
    string? RemoveReason);

public record CreateBlacklistRequest(
    int? CustomerId,
    [MaxLength(20)] string? IdentityNumber,
    [MaxLength(20)] string? PhoneNumber,
    BlacklistLevel Level,
    [MaxLength(500)] string? Reason);

public record RemoveBlacklistRequest([MaxLength(500)] string? Reason);

public record BlacklistFilterRequest(bool? ActiveOnly, int Page = 1, int PageSize = 20);

/// <summary>Kết quả sàng lọc khách: Level null nghĩa là không có bản ghi hiệu lực.</summary>
public record ScreeningResult(BlacklistLevel? Level, string? Reason);
