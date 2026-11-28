using CarRental.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController(IFileStorageService files) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{**key}")]
    public async Task<IActionResult> Get([FromRoute] string? key)
    {
        if (key is null || key.Contains("..", StringComparison.Ordinal) || key.Contains('\\')
            || key.Any(char.IsControl) || !key.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            || !(key.StartsWith("cars/", StringComparison.Ordinal) || key.StartsWith("avatars/", StringComparison.Ordinal)))
            return NotFound();

        var (content, contentType) = await files.OpenReadAsync(key);
        return File(content, contentType);
    }
}
