using CarRental.Application.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarRental.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ICustomerService _customerService;
    public AuthController(ICustomerService customerService) => _customerService = customerService;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        => Ok(await _customerService.RegisterAsync(request));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        => Ok(await _customerService.LoginAsync(request));

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetProfile()
        => Ok(await _customerService.GetProfileAsync(CurrentUserId));

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        await _customerService.UpdateProfileAsync(CurrentUserId, request);
        return NoContent();
    }

    [Authorize]
    [HttpPost("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await _customerService.ChangePasswordAsync(CurrentUserId, request);
        return NoContent();
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<ActionResult<AvatarDto>> UploadAvatar(IFormFile file)
        => Ok(await _customerService.UpdateAvatarAsync(CurrentUserId, file));
}
