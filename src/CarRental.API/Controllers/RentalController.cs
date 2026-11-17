using System.Security.Claims;
using CarRental.Application.Modules.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Authorize]
public class RentalController : ControllerBase
{
    private readonly IRentalService _rentalService;
    public RentalController(IRentalService rentalService) => _rentalService = rentalService;

    private int? CurrentUserId
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

    /// <summary>Customer chỉ thao tác booking của mình; Staff/Admin không bị giới hạn.</summary>
    private bool TryOwnerFilter(out int? customerId)
    {
        customerId = IsStaffOrAdmin ? null : CurrentUserId;
        return IsStaffOrAdmin || customerId != null;
    }

    [HttpPost("api/bookings/{bookingId:int}/payments")]
    public async Task<ActionResult<PaymentDto>> CreatePayment(int bookingId, CreatePaymentRequest request)
    {
        if (!TryOwnerFilter(out var customerId)) return Unauthorized();
        var dto = await _rentalService.CreatePaymentAsync(bookingId, request, customerId);
        return CreatedAtAction(nameof(GetPayments), new { bookingId }, dto);
    }

    [HttpGet("api/bookings/{bookingId:int}/payments")]
    public async Task<ActionResult<BookingPaymentsDto>> GetPayments(int bookingId)
    {
        if (!TryOwnerFilter(out var customerId)) return Unauthorized();
        return Ok(await _rentalService.GetPaymentsAsync(bookingId, customerId));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("api/payments/{paymentId:int}/mark-paid")]
    public async Task<ActionResult<PaymentDto>> MarkPaid(int paymentId, MarkPaymentPaidRequest request)
        => Ok(await _rentalService.MarkPaymentPaidAsync(paymentId, request));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("api/bookings/{bookingId:int}/pickup")]
    public async Task<ActionResult<BookingDto>> Pickup(int bookingId)
        => Ok(await _rentalService.PickupAsync(bookingId));

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("api/bookings/{bookingId:int}/return")]
    public async Task<ActionResult<ReturnRecordDto>> Return(int bookingId, ReturnBookingRequest request)
        => Ok(await _rentalService.ReturnAsync(bookingId, request, CurrentUserId));

    [HttpGet("api/bookings/{bookingId:int}/return")]
    public async Task<ActionResult<ReturnRecordDto>> GetReturn(int bookingId)
    {
        if (!TryOwnerFilter(out var customerId)) return Unauthorized();
        return Ok(await _rentalService.GetReturnAsync(bookingId, customerId));
    }

    /// <summary>Tải hợp đồng PDF. Customer chỉ tải booking của mình; Staff/Admin tải mọi booking đã xác nhận trở đi.</summary>
    [HttpGet("api/bookings/{bookingId:int}/contract")]
    public async Task<IActionResult> GetContract(int bookingId)
    {
        if (!TryOwnerFilter(out var customerId)) return Unauthorized();
        var file = await _rentalService.GetContractAsync(bookingId, customerId);
        return File(file.Content, "application/pdf", file.FileName);
    }
}
