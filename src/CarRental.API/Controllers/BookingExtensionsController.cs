using CarRental.API.Authorization;
using CarRental.API.Security;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Gia hạn thuê: khách gửi yêu cầu, Staff/Admin duyệt.</summary>
[ApiController]
[Authorize]
public class BookingExtensionsController : ControllerBase
{
    private readonly IBookingExtensionService _extensions;
    public BookingExtensionsController(IBookingExtensionService extensions) => _extensions = extensions;

    [HttpGet("api/bookings/{bookingId:int}/extend/check")]
    public async Task<ActionResult<ExtensionQuoteDto>> Check(int bookingId, [FromQuery] DateTime newEndDate)
    {
        if (!User.TryOwnerFilter(out var customerId)) return Unauthorized();
        return Ok(await _extensions.CheckAsync(bookingId, newEndDate, customerId));
    }

    [HttpPost("api/bookings/{bookingId:int}/extend")]
    public async Task<ActionResult<ExtensionDto>> Extend(int bookingId, ExtendBookingRequest request)
    {
        if (!User.TryOwnerFilter(out var customerId)) return Unauthorized();
        return StatusCode(StatusCodes.Status201Created, await _extensions.RequestAsync(bookingId, request, User.UserId()!.Value, customerId));
    }

    [HttpGet("api/bookings/{bookingId:int}/extensions")]
    public async Task<ActionResult<List<ExtensionDto>>> List(int bookingId)
    {
        if (!User.TryOwnerFilter(out var customerId)) return Unauthorized();
        return Ok(await _extensions.ListAsync(bookingId, customerId));
    }

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpGet("api/booking-extensions/pending")]
    public async Task<ActionResult<List<ExtensionDto>>> Pending() => Ok(await _extensions.ListPendingAsync());

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/booking-extensions/{id:int}/approve")]
    public async Task<ActionResult<ExtensionDto>> Approve(int id)
        => Ok(await _extensions.ApproveAsync(id, User.UserId()!.Value));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/booking-extensions/{id:int}/reject")]
    public async Task<ActionResult<ExtensionDto>> Reject(int id, RejectExtensionRequest request)
        => Ok(await _extensions.RejectAsync(id, request, User.UserId()!.Value));
}
