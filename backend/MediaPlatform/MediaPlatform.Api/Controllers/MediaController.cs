using MediaPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public MediaController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [Authorize]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File isn't upload");

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest("Only image files are supported");

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("Image size must be less than 10MB");

        await using var stream = file.OpenReadStream();
        var fileUrl = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        return Ok(new { url = fileUrl });
    }
}