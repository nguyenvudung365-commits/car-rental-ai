using CarRental.API.Authorization;
using CarRental.Application.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Route("api/admin/blacklist")]
public class BlacklistController : ControllerBase
{
    private readonly IBlacklistService _blacklist;
    private readonly ICustomerScreening _screening;

    public BlacklistController(IBlacklistService blacklist, ICustomerScreening screening)
    {
        _blacklist = blacklist;
        _screening = screening;
    }

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] BlacklistFilterRequest filter)
    {
        var (items, total) = await _blacklist.ListAsync(filter);
        return Ok(new { items, total });
    }

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpGet("check")]
    public async Task<ActionResult<ScreeningResult>> Check([FromQuery] int customerId)
        => Ok(await _screening.ScreenAsync(customerId));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<BlacklistEntryDto>> Create(CreateBlacklistRequest request)
        => StatusCode(StatusCodes.Status201Created, await _blacklist.CreateAsync(request));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:int}/remove")]
    public async Task<IActionResult> Remove(int id, RemoveBlacklistRequest request)
    { await _blacklist.RemoveAsync(id, request); return NoContent(); }
}
