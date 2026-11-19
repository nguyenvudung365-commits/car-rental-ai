using CarRental.Application.Modules.Customers;
using CarRental.Domain.Common;
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

    public async Task<(List<CustomerEntity> Items, int TotalCount)> SearchAsync(
        UserRole? role, string? search, bool? isLocked, int page, int pageSize)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();
        if (role.HasValue) query = query.Where(c => c.Role == role);
        if (isLocked.HasValue) query = query.Where(c => c.IsLocked == isLocked);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || c.PhoneNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<int> CountActiveAdminsAsync()
        => _context.Customers.CountAsync(c => c.Role == UserRole.Admin && !c.IsLocked);
}