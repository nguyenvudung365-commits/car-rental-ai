using CarRental.API.Authorization;
using CarRental.Application.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _users;
    public AdminUsersController(IUserAdminService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserListFilterRequest filter)
    {
        var (items, total) = await _users.ListAsync(filter);
        return Ok(new { items, total });
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> CreateStaff(CreateStaffRequest request)
    {
        var dto = await _users.CreateStaffAsync(request);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest request)
    { await _users.ChangeRoleAsync(id, request); return NoContent(); }

    [HttpPost("{id:int}/lock")]
    public async Task<IActionResult> Lock(int id, LockUserRequest request)
    { await _users.LockAsync(id, request); return NoContent(); }

    [HttpPost("{id:int}/unlock")]
    public async Task<IActionResult> Unlock(int id)
    { await _users.UnlockAsync(id); return NoContent(); }
}
