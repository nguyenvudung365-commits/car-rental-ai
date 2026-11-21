using CarRental.Application.Modules.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    public ReportsController(ReportService reportService) => _reportService = reportService;

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _reportService.GetRevenueAsync(from, to));

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyReportDto>> Occupancy([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _reportService.GetOccupancyAsync(from, to));
}
