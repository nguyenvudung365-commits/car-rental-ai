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

    /// <summary>Tài khoản bị Admin khóa: không đăng nhập, token cũ bị từ chối.</summary>
    public bool IsLocked { get; set; }

    public DateTime? LockedAt { get; set; }

    public string? LockReason { get; set; }

    /// <summary>Object key ảnh đại diện (prefix avatars/, đọc công khai).</summary>
    public string? AvatarKey { get; set; }

    /// <summary>Số dư điểm thưởng hiện có.</summary>
    public int LoyaltyPoints { get; set; }

    /// <summary>Tổng điểm đã tích lũy trọn đời (không trừ khi tiêu), dùng để xếp hạng.</summary>
    public int LifetimePoints { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
