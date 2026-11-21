using System.Security.Claims;
using CarRental.Application.Common;

namespace CarRental.API.Security;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var id = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return int.TryParse(id, out var v) ? v : null;
        }
    }

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool IsAdmin => Principal?.IsInRole("Admin") == true;
    public bool IsStaffOrAdmin => IsAdmin || Principal?.IsInRole("Staff") == true;
}
