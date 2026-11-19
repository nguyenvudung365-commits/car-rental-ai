using CarRental.API.Authorization;
using CarRental.Application.Modules.Cars;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Nhập xe hàng loạt từ xlsx. Staff nhập thì xe ChoDuyet, Admin thì SanSang (như tạo xe thủ công).</summary>
[ApiController]
[Authorize(Roles = AppRoles.StaffOrAdmin)]
[Route("api/cars/import")]
public class CarImportController : ControllerBase
{
    private readonly CarImportService _import;
    public CarImportController(CarImportService import) => _import = import;

    [HttpGet("template")]
    public IActionResult Template()
        => File(_import.Template(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mau-nhap-xe.xlsx");

    [HttpPost]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<ImportResultDto>> Import(IFormFile file, [FromQuery] bool dryRun = true)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await _import.ImportAsync(stream, file.Length, file.FileName, dryRun, User.IsInRole(AppRoles.Admin)));
    }
}
