using MediaPlatform.Media.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaPlatform.Media.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;

    public MediaController(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    [Authorize]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File not provided");
        if (!file.ContentType.StartsWith("image/")) return BadRequest("Only image files are supported");

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync(stream, file.FileName, file.ContentType);
        return Ok(new { url });
    }
}