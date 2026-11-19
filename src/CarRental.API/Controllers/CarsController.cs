using CarRental.API.Authorization;
using CarRental.Application.Modules.Cars;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;
    public CarsController(ICarService carService) => _carService = carService;

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] CarFilterRequest filter)
    {
        var (items, total) = await _carService.GetFilteredAsync(filter, includePending: User.IsInRole("Admin") || User.IsInRole("Staff"));
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CarDto>> GetById(int id)
    {
        var car = await _carService.GetByIdAsync(id, includePending: User.IsInRole("Admin") || User.IsInRole("Staff"));
        return car is null ? NotFound() : Ok(car);
    }

    /// <summary>Staff tạo thì xe ở trạng thái ChoDuyet (giá chỉ là đề xuất); Admin tạo thì xe SanSang luôn.</summary>
    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<CarDto>> Create(CreateCarRequest request)
        => Ok(await _carService.CreateAsync(request, User.IsInRole(AppRoles.Admin)));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCarRequest request)
    { await _carService.UpdateAsync(id, request, User.IsInRole(AppRoles.Admin)); return NoContent(); }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<CarDto>> Approve(int id, ApproveCarRequest? request)
        => Ok(await _carService.ApproveAsync(id, request));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _carService.DeleteAsync(id); return NoContent(); }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{carId}/images")]
    public async Task<ActionResult<CarImageDto>> UploadImage(int carId, IFormFile file)
        => Ok(await _carService.AddImageAsync(carId, file));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{carId}/images/{imageId}/primary")]
    public async Task<IActionResult> SetPrimaryImage(int carId, int imageId)
    { await _carService.SetPrimaryImageAsync(carId, imageId); return NoContent(); }

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("{carId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(int carId, int imageId)
    { await _carService.DeleteImageAsync(carId, imageId); return NoContent(); }
}