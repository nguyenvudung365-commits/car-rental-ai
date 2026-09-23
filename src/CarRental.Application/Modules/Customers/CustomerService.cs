using CarRental.Application.Common;
using CarRental.Domain.Common;
using CarRental.Application.Modules.Customers;
using CustomerEntity = CarRental.Domain.Modules.Customers.Customer;

namespace CarRental.Application.Modules.Customers;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _customerRepository.GetByEmailAsync(normalizedEmail) is not null)
            throw new ConflictException("EMAIL_EXISTS", "Email đã được sử dụng.");

        if (await _customerRepository.GetByPhoneNumberAsync(request.PhoneNumber) is not null)
            throw new ConflictException("PHONE_EXISTS", "Số điện thoại đã được sử dụng.");

        var customer = new CustomerEntity
        {
            FullName = request.FullName,
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Customer
        };

        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(customer);
        return new AuthResponse(token, customer.FullName, customer.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var customer = await _customerRepository.GetByEmailAsync(normalizedEmail)
            ?? throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        if (!_passwordHasher.Verify(request.Password, customer.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        var token = _jwtTokenGenerator.GenerateToken(customer);
        return new AuthResponse(token, customer.FullName, customer.Role.ToString());
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        return customer is null
            ? null
            : new CustomerDto(customer.Id, customer.FullName, customer.Email, customer.PhoneNumber, customer.Role.ToString(), customer.IdentityNumber, customer.Address);
    }

    public async Task UpdateProfileAsync(int customerId, UpdateProfileRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Không tìm thấy khách hàng.");

        customer.FullName = request.FullName;
        customer.IdentityNumber = request.IdentityNumber;
        customer.Address = request.Address;

        // cần thêm Update(customer) vào ICustomerRepository nếu chưa có
        await _unitOfWork.SaveChangesAsync();
    }
}