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
        var (items, total) = await _carService.GetFilteredAsync(filter);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CarDto>> GetById(int id)
    {
        var car = await _carService.GetByIdAsync(id);
        return car is null ? NotFound() : Ok(car);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<ActionResult<CarDto>> Create(CreateCarRequest request)
        => Ok(await _carService.CreateAsync(request));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCarRequest request)
    { await _carService.UpdateAsync(id, request); return NoContent(); }

    [Authorize(Roles = "Admin,Staff")]
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
}