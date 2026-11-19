using CarRental.Domain.Common;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Application.Modules.Customers;

public record CustomerListFilterRequest(
    string? Search,
    MemberTier? Tier,
    DocumentsOverallStatus? DocStatus,
    bool? Blacklisted,
    int Page = 1,
    int PageSize = 20);

public record CustomerListItemDto(
    int Id,
    string FullName,
    string Email,
    string PhoneNumber,
    MemberTier Tier,
    int LoyaltyPoints,
    DocumentsOverallStatus DocumentsStatus,
    BlacklistLevel? BlacklistLevel,
    bool IsLocked,
    int BookingCount,
    decimal TotalSpent,
    DateTime CreatedAt);

public record CustomerBookingBriefDto(int Id, string CarName, DateTime StartDate, DateTime EndDate, decimal TotalPrice, string Status);

/// <summary>Staff nhận bản rút gọn: IdentityNumber, Address, LockReason là null và Loyalty.History rỗng.</summary>
public record CustomerDetailDto(
    int Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string? IdentityNumber,
    string? Address,
    string? AvatarKey,
    bool IsLocked,
    string? LockReason,
    DateTime? LockedAt,
    DateTime CreatedAt,
    int BookingCount,
    decimal TotalSpent,
    DocumentsOverallStatus DocumentsStatus,
    ScreeningResult Blacklist,
    LoyaltyDto Loyalty,
    List<DocumentDto> Documents,
    List<CustomerBookingBriefDto> RecentBookings);

/// <summary>Một dòng danh sách khách kèm số liệu tổng hợp do repository tính.</summary>
public record CustomerListRow(
    CustomerEntity Customer,
    DocumentsOverallStatus DocumentsStatus,
    BlacklistLevel? BlacklistLevel,
    int BookingCount,
    decimal TotalSpent);
