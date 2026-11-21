using CarRental.Domain.Common;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Application.Modules.Customers;

public interface ICustomerRepository
{
    Task<CustomerEntity?> GetByEmailAsync(string email);
    Task<CustomerEntity?> GetByPhoneNumberAsync(string phoneNumber);
    Task<CustomerEntity?> GetByIdAsync(int id);
    Task AddAsync(CustomerEntity customer);

    void Update(CustomerEntity customer);

    Task<(List<CustomerEntity> Items, int TotalCount)> SearchAsync(UserRole? role, string? search, bool? isLocked, int page, int pageSize);
    Task<int> CountActiveAdminsAsync();
}