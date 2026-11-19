using CarRental.API.Authorization;
using CarRental.Application.Modules.Cars;
using CarRental.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Lịch bảo dưỡng/đăng kiểm/bảo hiểm. Staff và Admin; chi phí chỉ Admin thấy.</summary>
[ApiController]
[Authorize(Roles = AppRoles.StaffOrAdmin)]
[Route("api/maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _service;
    public MaintenanceController(IMaintenanceService service) => _service = service;

    private bool IsAdmin => User.IsInRole(AppRoles.Admin);

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceDto>>> List([FromQuery] int? carId, [FromQuery] MaintenanceStatus? status)
        => Ok(await _service.ListAsync(new MaintenanceListFilter(carId, status), IsAdmin));

    [HttpGet("due")]
    public async Task<ActionResult<MaintenanceDueDto>> Due([FromQuery] int withinDays = 30)
        => Ok(await _service.GetDueAsync(withinDays, IsAdmin));

    [HttpPost]
    public async Task<ActionResult<MaintenanceDto>> Create(CreateMaintenanceRequest request)
        => StatusCode(StatusCodes.Status201Created, await _service.CreateAsync(request, IsAdmin));

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<MaintenanceDto>> Start(int id, StartMaintenanceRequest? request)
        => Ok(await _service.StartAsync(id, request?.Force ?? false, IsAdmin));

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<MaintenanceDto>> Complete(int id, CompleteMaintenanceRequest request)
        => Ok(await _service.CompleteAsync(id, request, IsAdmin));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<MaintenanceDto>> Cancel(int id)
        => Ok(await _service.CancelAsync(id, IsAdmin));
}
