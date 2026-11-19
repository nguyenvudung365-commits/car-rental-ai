using CarRental.API.Authorization;
using CarRental.Application.Modules.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogReader _reader;
    public AuditLogsController(IAuditLogReader reader) => _reader = reader;

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] AuditLogFilterRequest filter)
    {
        var (items, total) = await _reader.QueryAsync(filter);
        return Ok(new { items, total });
    }
}
