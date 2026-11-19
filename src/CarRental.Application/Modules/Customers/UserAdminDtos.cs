using System.ComponentModel.DataAnnotations;
using CarRental.Domain.Common;

namespace CarRental.Application.Modules.Customers;

public record UserSummaryDto(
    int Id,
    string FullName,
    string Email,
    string PhoneNumber,
    UserRole Role,
    bool IsLocked,
    DateTime CreatedAt,
    string? LockReason = null);

public record UserListFilterRequest(
    UserRole? Role,
    string? Search,
    bool? IsLocked,
    int Page = 1,
    int PageSize = 20);

public record CreateStaffRequest(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, Phone] string PhoneNumber,
    [Required, MinLength(8)] string Password);

public record ChangeRoleRequest(UserRole Role);

public record LockUserRequest(string? Reason);
