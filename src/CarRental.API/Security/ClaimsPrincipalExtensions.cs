using System.Security.Claims;

namespace CarRental.API.Security;

public static class ClaimsPrincipalExtensions
{
    public static int? UserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.TryParse(id, out var v) ? v : null;
    }

    public static bool IsStaffOrAdmin(this ClaimsPrincipal user) => user.IsInRole("Staff") || user.IsInRole("Admin");

    /// <summary>Khách chỉ thao tác đơn của mình (trả id khách); Staff/Admin không bị giới hạn (null). False khi không xác định được người dùng.</summary>
    public static bool TryOwnerFilter(this ClaimsPrincipal user, out int? customerId)
    {
        customerId = user.IsStaffOrAdmin() ? null : user.UserId();
        return user.IsStaffOrAdmin() || customerId != null;
    }
}
