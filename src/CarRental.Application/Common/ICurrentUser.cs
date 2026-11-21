namespace CarRental.Application.Common;

/// <summary>Người dùng của request hiện tại (null khi chạy ngoài HTTP như seed).</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Role { get; }
    string? IpAddress { get; }
    bool IsAdmin { get; }
    bool IsStaffOrAdmin { get; }
}
