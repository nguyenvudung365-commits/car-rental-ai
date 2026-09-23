using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.Modules.Customers;

public record RegisterRequest(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, Phone] string PhoneNumber,
    [Required, MinLength(8)] string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string FullName, string Role);
public record CustomerDto(int Id, string FullName, string Email, string PhoneNumber, string Role, string? IdentityNumber, string? Address);

public record UpdateProfileRequest(string FullName, string? IdentityNumber, string? Address);