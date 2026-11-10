using System.Security.Claims;
using MediaPlatform.Media.Api.Data;
using MediaPlatform.Media.Api.Entities;
using MediaPlatform.Media.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Media.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly MediaDbContext _context;
    private readonly VideoProcessingQueue _queue;

    public VideosController(MediaDbContext context, VideoProcessingQueue queue)
    {
        _context = context;
        _queue = queue;
    }

    [HttpGet]
    public async Task<IActionResult> GetVideos([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        var query = _context.Videos.AsNoTracking().Where(v => v.Status == VideoStatus.Ready).OrderByDescending(v => v.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new VideoDto(
                v.Id, v.Title, v.Description, v.VideoUrl, v.ThumbnailUrl, v.DurationSeconds, v.ViewsCount, v.CreatedAt,
                new AuthorDto(v.AuthorId, v.AuthorName, v.AuthorDisplayName)
            )).ToListAsync();

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetVideoById(Guid id)
    {
        var video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == id);
        if (video == null) return NotFound("video not found");

        video.ViewsCount++;
        await _context.SaveChangesAsync();

        return Ok(new VideoDto(
            video.Id, video.Title, video.Description, video.VideoUrl, video.ThumbnailUrl, video.DurationSeconds, video.ViewsCount, video.CreatedAt,
            new AuthorDto(video.AuthorId, video.AuthorName, video.AuthorDisplayName)
        ));
    }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<IActionResult> UploadVideo([FromForm] UploadVideoRequest request)
    {
        if (request.File == null || request.File.Length == 0) return BadRequest("file not provided");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var displayName = User.FindFirstValue("displayName") ?? userName;

        var tempFilePath = Path.GetTempFileName();
        await using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        var video = new Video
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = userId,
            AuthorName = userName,
            AuthorDisplayName = displayName,
            Status = VideoStatus.Processing
        };

        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        _queue.Enqueue(new VideoProcessingTask(video.Id, tempFilePath, request.File.FileName));
        return Accepted(new { id = video.Id, message = "video processing" });
    }
}

public record UploadVideoRequest(string Title, string? Description, IFormFile File);
public record AuthorDto(Guid Id, string UserName, string DisplayName);
public record VideoDto(Guid Id, string Title, string? Description, string VideoUrl, string? ThumbnailUrl, double DurationSeconds, long ViewsCount, DateTime CreatedAt, AuthorDto Author);