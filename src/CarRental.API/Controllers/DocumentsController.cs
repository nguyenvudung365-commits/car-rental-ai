using System.Security.Claims;
using CarRental.API.Authorization;
using CarRental.Application.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

/// <summary>Giấy tờ CCCD/bằng lái: khách tự nộp, Staff/Admin duyệt. Ảnh ở prefix private/, chỉ đọc qua endpoint file.</summary>
[ApiController]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;
    public DocumentsController(IDocumentService documents) => _documents = documents;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet("api/me/documents")]
    public async Task<ActionResult<List<DocumentDto>>> GetMine() => Ok(await _documents.GetMineAsync(CurrentUserId));

    [HttpPost("api/me/documents")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<DocumentDto>> Submit([FromForm] SubmitDocumentRequest request)
        => StatusCode(StatusCodes.Status201Created, await _documents.SubmitAsync(CurrentUserId, request));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpGet("api/admin/documents")]
    public async Task<IActionResult> Queue([FromQuery] DocumentFilterRequest filter)
    {
        var (items, total) = await _documents.ListQueueAsync(filter);
        return Ok(new { items, total });
    }

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/admin/documents/{id:int}/approve")]
    public async Task<ActionResult<DocumentDto>> Approve(int id) => Ok(await _documents.ApproveAsync(id));

    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [HttpPost("api/admin/documents/{id:int}/reject")]
    public async Task<ActionResult<DocumentDto>> Reject(int id, RejectDocumentRequest request)
        => Ok(await _documents.RejectAsync(id, request));

    /// <summary>Chủ sở hữu hoặc Staff/Admin (kiểm trong service). Không cache vì là ảnh giấy tờ.</summary>
    [HttpGet("api/documents/{id:int}/file")]
    public async Task<IActionResult> GetFile(int id, [FromQuery] string side = "front")
    {
        var file = await _documents.GetFileAsync(id, side);
        Response.Headers.CacheControl = "private, no-store";
        return File(file.Content, file.ContentType);
    }
}
