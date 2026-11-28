using CarRental.API.Authorization;
using CarRental.Application.Modules.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.StaffOrAdmin)]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardQuery _query;
    public DashboardController(IDashboardQuery query) => _query = query;

    /// <summary>TodayRevenue chỉ trả cho Admin.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardDto>> Summary()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await _query.GetAsync(DateTime.UtcNow, User.IsInRole(AppRoles.Admin)));
    }
}
