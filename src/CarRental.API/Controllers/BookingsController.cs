using System.Security.Claims;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    public BookingsController(IBookingService bookingService) => _bookingService = bookingService;

    private int? CurrentCustomerId
    {
        get
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue("sub");
            return int.TryParse(id, out var v) ? v : null;
        }
    }

    private bool IsStaffOrAdmin => User.IsInRole("Staff") || User.IsInRole("Admin");

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request)
    {
        var customerId = CurrentCustomerId;
        if (customerId == null) return Unauthorized();
        var dto = await _bookingService.CreateAsync(customerId.Value, request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] BookingFilterRequest filter)
    {
        int? customerId = IsStaffOrAdmin ? null : CurrentCustomerId;
        if (!IsStaffOrAdmin && customerId == null) return Unauthorized();
        var (items, total) = await _bookingService.GetFilteredAsync(filter, customerId);
        return Ok(new { items, total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDto>> GetById(int id)
    {
        int? customerId = IsStaffOrAdmin ? null : CurrentCustomerId;
        if (!IsStaffOrAdmin && customerId == null) return Unauthorized();
        var dto = await _bookingService.GetByIdAsync(id, customerId);
        return Ok(dto);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<BookingDto>> Confirm(int id)
        => Ok(await _bookingService.ConfirmAsync(id));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(int id, CancelBookingRequest? request)
    {
        if (!IsStaffOrAdmin)
        {
            var customerId = CurrentCustomerId;
            if (customerId == null) return Unauthorized();
            var booking = await _bookingService.GetByIdAsync(id, customerId);
            _ = booking;
        }
        var dto = await _bookingService.CancelAsync(id, request?.Reason);
        return Ok(dto);
    }
}
