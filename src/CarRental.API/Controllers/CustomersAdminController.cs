using CarRental.API.Authorization;
using CarRental.Application.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Quản lý khách: Staff và Admin đọc (Staff nhận bản rút gọn); khóa/chặn/điều chỉnh điểm dùng endpoint của Admin.</summary>
[ApiController]
[Authorize(Roles = AppRoles.StaffOrAdmin)]
[Route("api/admin/customers")]
public class CustomersAdminController : ControllerBase
{
    private readonly ICustomerAdminService _customers;
    public CustomersAdminController(ICustomerAdminService customers) => _customers = customers;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] CustomerListFilterRequest filter)
    {
        var (items, total) = await _customers.ListAsync(filter);
        return Ok(new { items, total });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(int id) => Ok(await _customers.GetAsync(id));
}
