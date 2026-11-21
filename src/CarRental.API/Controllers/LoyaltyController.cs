using System.Security.Claims;
using CarRental.API.Authorization;
using CarRental.Application.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyalty;
    public LoyaltyController(ILoyaltyService loyalty) => _loyalty = loyalty;

    [HttpGet("api/me/loyalty")]
    public async Task<ActionResult<LoyaltyDto>> GetMine()
        => Ok(await _loyalty.GetAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!)));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("api/admin/customers/{id:int}/loyalty/adjust")]
    public async Task<ActionResult<LoyaltyDto>> Adjust(int id, AdjustLoyaltyRequest request)
        => Ok(await _loyalty.AdjustAsync(id, request));
}
