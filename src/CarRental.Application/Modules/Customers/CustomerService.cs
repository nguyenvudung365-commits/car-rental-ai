using CarRental.Application.Common;
using Microsoft.AspNetCore.Http;
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
    private readonly IDocumentRepository _documents;
    private readonly IFileStorageService _files;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork,
        IDocumentRepository documents,
        IFileStorageService files)
    {
        _documents = documents;
        _files = files;
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

        if (customer.IsLocked)
            throw new ForbiddenException("ACCOUNT_LOCKED", "Tài khoản đã bị khóa. Vui lòng liên hệ cửa hàng.");

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

        // Đổi số CCCD sau khi CCCD đã được duyệt thì phải duyệt lại.
        var newNumber = IdentityNormalizer.Cccd(request.IdentityNumber);
        if (newNumber != IdentityNormalizer.Cccd(customer.IdentityNumber)
            && await _documents.GetActiveAsync(customerId, DocumentType.Cccd) is { Status: DocumentStatus.DaDuyet } cccd
            && cccd.Number != newNumber)
        {
            cccd.Status = DocumentStatus.ChoDuyet;
            cccd.ReviewedById = null;
            cccd.ReviewedAt = null;
        }

        customer.FullName = request.FullName;
        customer.IdentityNumber = request.IdentityNumber;
        customer.Address = request.Address;

        // cần thêm Update(customer) vào ICustomerRepository nếu chưa có
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<ProfileDto> GetProfileAsync(int customerId)
    {
        var c = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Không tìm thấy khách hàng.");
        var docs = await _documents.GetByCustomerAsync(customerId);
        return new ProfileDto(
            c.Id, c.FullName, c.Email, c.PhoneNumber, c.Role.ToString(), c.IdentityNumber, c.Address, c.AvatarKey,
            c.LoyaltyPoints, c.LifetimePoints, LoyaltyPolicy.TierFor(c.LifetimePoints), DocumentService.Overall(docs));
    }

    public async Task ChangePasswordAsync(int customerId, ChangePasswordRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Không tìm thấy khách hàng.");
        if (!_passwordHasher.Verify(request.CurrentPassword, customer.PasswordHash))
            throw new BadRequestException("WRONG_PASSWORD", "Mật khẩu hiện tại không đúng.");
        if (request.CurrentPassword == request.NewPassword)
            throw new BadRequestException("SAME_PASSWORD", "Mật khẩu mới phải khác mật khẩu hiện tại.");

        customer.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<AvatarDto> UpdateAvatarAsync(int customerId, IFormFile file)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Không tìm thấy khách hàng.");
        var oldKey = customer.AvatarKey;
        customer.AvatarKey = await _files.SaveAsync(file, $"avatars/{customerId}");
        await _unitOfWork.SaveChangesAsync();

        // Xóa ảnh cũ là việc phụ: lỗi (key lạ, mất kết nối) không làm hỏng việc đổi avatar.
        if (oldKey is not null)
            try { _files.Delete(oldKey); } catch (Exception) { /* bỏ qua */ }
        return new AvatarDto(customer.AvatarKey);
    }
}
