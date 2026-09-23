namespace CarRental.Application.Modules.Customers;

/// <summary>Auth + hồ sơ khách — TV B implement theo docs/api-contract.md mục 1.</summary>
public interface ICustomerService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<CustomerDto?> GetByIdAsync(int id);
    Task UpdateProfileAsync(int customerId, UpdateProfileRequest request);
}

