using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Application.Common;

public interface IJwtTokenGenerator
{
    string GenerateToken(CustomerEntity customer);
}