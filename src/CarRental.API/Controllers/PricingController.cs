using CarRental.API.Authorization;
using CarRental.API.Security;
using CarRental.Application.Modules.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Mã giảm giá, phụ phí, chi nhánh. Chỉ Admin tạo/sửa; khách chỉ kiểm tra mã và xem chi nhánh.</summary>
[ApiController]
public class PricingController : ControllerBase
{
    private readonly PricingAdminService _pricing;
    public PricingController(PricingAdminService pricing) => _pricing = pricing;

    [Authorize]
    [HttpGet("api/coupons/check")]
    public async Task<ActionResult<CouponCheckDto>> Check([FromQuery] string code, [FromQuery] decimal? orderAmount)
    {
        var customerId = User.UserId();
        return customerId is null ? Unauthorized() : Ok(await _pricing.CheckCouponAsync(customerId.Value, code, orderAmount));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/admin/coupons")]
    public async Task<ActionResult<List<CouponDto>>> Coupons() => Ok(await _pricing.ListCouponsAsync());

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("api/admin/coupons")]
    public async Task<ActionResult<CouponDto>> CreateCoupon(SaveCouponRequest request)
        => StatusCode(StatusCodes.Status201Created, await _pricing.CreateCouponAsync(request));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("api/admin/coupons/{id:int}")]
    public async Task<ActionResult<CouponDto>> UpdateCoupon(int id, SaveCouponRequest request)
        => Ok(await _pricing.UpdateCouponAsync(id, request));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/admin/surcharges")]
    public async Task<ActionResult<List<SurchargeDto>>> Surcharges() => Ok(await _pricing.ListSurchargesAsync());

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("api/admin/surcharges")]
    public async Task<ActionResult<SurchargeDto>> CreateSurcharge(SaveSurchargeRequest request)
        => StatusCode(StatusCodes.Status201Created, await _pricing.CreateSurchargeAsync(request));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("api/admin/surcharges/{id:int}")]
    public async Task<ActionResult<SurchargeDto>> UpdateSurcharge(int id, SaveSurchargeRequest request)
        => Ok(await _pricing.UpdateSurchargeAsync(id, request));

    [AllowAnonymous]
    [HttpGet("api/branches")]
    public async Task<ActionResult<List<BranchDto>>> Branches() => Ok(await _pricing.ListBranchesAsync(false));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/admin/branches")]
    public async Task<ActionResult<List<BranchDto>>> AllBranches() => Ok(await _pricing.ListBranchesAsync(true));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("api/admin/branches")]
    public async Task<ActionResult<BranchDto>> CreateBranch(SaveBranchRequest request)
        => StatusCode(StatusCodes.Status201Created, await _pricing.CreateBranchAsync(request));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("api/admin/branches/{id:int}")]
    public async Task<ActionResult<BranchDto>> UpdateBranch(int id, SaveBranchRequest request)
        => Ok(await _pricing.UpdateBranchAsync(id, request));
}
