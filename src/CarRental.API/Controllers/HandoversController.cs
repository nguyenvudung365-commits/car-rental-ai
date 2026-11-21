using CarRental.API.Authorization;
using CarRental.API.Security;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Biên bản giao/nhận xe. Ảnh ở prefix private/, chỉ đọc qua endpoint ảnh có kiểm quyền.</summary>
[ApiController]
[Authorize]
public class HandoversController : ControllerBase
{
    private readonly IHandoverService _handovers;
    public HandoversController(IHandoverService handovers) => _handovers = handovers;

    [HttpGet("api/bookings/{bookingId:int}/handovers")]
    public async Task<ActionResult<List<HandoverDto>>> List(int bookingId)
    {
        if (!User.TryOwnerFilter(out var customerId)) return Unauthorized();
        return Ok(await _handovers.ListAsync(bookingId, customerId));
    }

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/bookings/{bookingId:int}/handovers")]
    public async Task<ActionResult<HandoverDto>> Create(int bookingId, CreateHandoverRequest request)
        => StatusCode(StatusCodes.Status201Created, await _handovers.CreateAsync(bookingId, request, User.UserId()));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/handovers/{id:int}/photos")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<HandoverPhotoDto>> AddPhoto(int id, IFormFile file)
        => StatusCode(StatusCodes.Status201Created, await _handovers.AddPhotoAsync(id, file));

    /// <summary>Chủ đơn hoặc Staff/Admin. Không cache vì là ảnh biên bản.</summary>
    [HttpGet("api/handover-photos/{id:int}")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        if (!User.TryOwnerFilter(out var customerId)) return Unauthorized();
        var file = await _handovers.GetPhotoAsync(id, customerId);
        Response.Headers.CacheControl = "private, no-store";
        return File(file.Content, file.ContentType);
    }
}
