using CarRental.Application.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Infrastructure.Persistence.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    public CustomerRepository(ApplicationDbContext context) => _context = context;

    public Task<CustomerEntity?> GetByEmailAsync(string email)
        => _context.Customers
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Email == email);

    public Task<CustomerEntity?> GetByPhoneNumberAsync(string phoneNumber)
        => _context.Customers
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.PhoneNumber == NormalizePhone(phoneNumber));

    private static string NormalizePhone(string phone)
    {
        var normalized = phone.Replace(" ", "").Replace("-", "");
        if (normalized.StartsWith("+84"))
            return "0" + normalized.Substring(3);
        if (normalized.StartsWith("84"))
            return "0" + normalized.Substring(2);
        return normalized;
    }

    public async Task<CustomerEntity?> GetByIdAsync(int id)
        => await _context.Customers
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(CustomerEntity customer)
        => await _context.Customers.AddAsync(customer);

    public void Update(CustomerEntity customer) => _context.Customers.Update(customer);
}