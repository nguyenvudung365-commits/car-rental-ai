using CarRental.API.Authorization;
using CarRental.Application.Modules.Bookings;
using CarRental.Application.Modules.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Xuất Excel/PDF. Báo cáo chỉ Admin; danh sách đơn Staff và Admin (tối đa 5.000 dòng).</summary>
[ApiController]
public class ReportExportsController : ControllerBase
{
    private readonly ReportExportService _export;
    public ReportExportsController(ReportExportService export) => _export = export;

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/reports/revenue/export")]
    public async Task<IActionResult> Revenue([FromQuery] string? format, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Download(await _export.RevenueAsync(format, from, to));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/reports/occupancy/export")]
    public async Task<IActionResult> Occupancy([FromQuery] string? format, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Download(await _export.OccupancyAsync(format, from, to));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpGet("api/bookings/export")]
    public async Task<IActionResult> Bookings([FromQuery] BookingFilterRequest filter)
        => Download(await _export.BookingsAsync(filter));

    private FileContentResult Download(ExportFile f)
    {
        Response.Headers.CacheControl = "no-store";
        return File(f.Content, f.ContentType, f.FileName);
    }
}
