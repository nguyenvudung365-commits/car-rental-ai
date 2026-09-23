using CarRental.Domain.Common;
using CarRental.Domain.Modules.Bookings;

namespace CarRental.Domain.Modules.Customers;

/// <summary>Bảng Customer — người thuê xe, cũng là tài khoản đăng nhập.</summary>
public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Số điện thoại — duy nhất (lỗi 409 PHONE_EXISTS).</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Email — duy nhất (lỗi 409 EMAIL_EXISTS).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Mật khẩu đã hash (BCrypt/Argon2). KHÔNG bao giờ lưu plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Số CCCD/CMND — cần cho hợp đồng thuê xe.</summary>
    public string? IdentityNumber { get; set; }

    public string? Address { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
