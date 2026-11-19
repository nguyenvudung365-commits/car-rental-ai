using CarRental.API.Authorization;
using CarRental.Application.Modules.Cars;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.StaffOrAdmin)]
public class CalendarController : ControllerBase
{
    private readonly CalendarService _calendar;
    public CalendarController(CalendarService calendar) => _calendar = calendar;

    /// <summary>from/to là mốc thời gian UTC; tối đa 62 ngày.</summary>
    [HttpGet("api/cars/calendar")]
    public async Task<ActionResult<List<CarCalendarDto>>> Get([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? branchId)
        => Ok(await _calendar.GetAsync(from, to, branchId));
}
