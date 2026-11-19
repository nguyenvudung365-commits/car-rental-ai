using CarRental.API.Authorization;
using CarRental.API.Security;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Cọc bảo đảm: tách khỏi tiền thuê, không phải doanh thu.</summary>
[ApiController]
[Authorize]
public class DepositsController : ControllerBase
{
    private readonly IDepositService _deposits;
    public DepositsController(IDepositService deposits) => _deposits = deposits;

    [HttpGet("api/bookings/{bookingId:int}/deposit")]
    public async Task<ActionResult<DepositSummaryDto>> Get(int bookingId)
    {
        if (!User.TryOwnerFilter(out var customerId)) return Unauthorized();
        return Ok(await _deposits.GetAsync(bookingId, customerId));
    }

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/bookings/{bookingId:int}/deposit/collect")]
    public async Task<ActionResult<DepositSummaryDto>> Collect(int bookingId, CollectDepositRequest request)
        => Ok(await _deposits.CollectAsync(bookingId, request, User.UserId()));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/bookings/{bookingId:int}/deposit/settle")]
    public async Task<ActionResult<DepositSummaryDto>> Settle(int bookingId, SettleDepositRequest request)
        => Ok(await _deposits.SettleAsync(bookingId, request, User.UserId()));
}
