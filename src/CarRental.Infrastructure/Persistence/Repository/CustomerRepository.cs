using CarRental.Application.Modules.Customers;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Infrastructure.Persistence.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;
    public CustomerRepository(ApplicationDbContext context) => _context = context;

    public Task<CustomerEntity?> GetByEmailAsync(string email)
        => _context.Customers.FirstOrDefaultAsync(c => c.Email == email);

    public Task<CustomerEntity?> GetByPhoneNumberAsync(string phoneNumber)
    => _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

    public async Task<CustomerEntity?> GetByIdAsync(int id)
        => await _context.Customers.FindAsync(id);

    public async Task AddAsync(CustomerEntity customer)
        => await _context.Customers.AddAsync(customer);

    public void Update(CustomerEntity customer) => _context.Customers.Update(customer);
}