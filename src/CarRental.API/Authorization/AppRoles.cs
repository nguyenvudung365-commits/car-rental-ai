namespace CarRental.API.Authorization;

/// <summary>Chuỗi vai trò dùng cho [Authorize(Roles = ...)] để không gõ sai.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string StaffOrAdmin = "Admin,Staff";
}
