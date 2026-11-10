using System.Security.Claims;
using MediaPlatform.Core.Entities;
using MediaPlatform.Infrastructure.Data;
using MediaPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly VideoProcessingQueue _queue;

    public VideosController(AppDbContext context, VideoProcessingQueue queue)
    {
        _context = context;
        _queue = queue;
    }

    [HttpGet]
    public async Task<IActionResult> GetVideos([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        var query = _context.Videos
            .AsNoTracking()
            .Where(v => v.Status == VideoStatus.Ready)
            .OrderByDescending(v => v.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VideoDto(
                v.Id,
                v.Title,
                v.Description,
                v.VideoUrl,
                v.ThumbnailUrl,
                v.DurationSeconds,
                v.ViewsCount,
                v.CreatedAt,
                new AuthorDto(v.Author.Id, v.Author.UserName ?? "", v.Author.DisplayName)
            ))
            .ToListAsync();

        return Ok(new PagedResult<VideoDto>(items, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetVideoById(Guid id)
    {
        var video = await _context.Videos
            .Include(v => v.Author)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (video == null) return NotFound("Video Not Found");

        video.ViewsCount++;
        await _context.SaveChangesAsync();

        return Ok(new VideoDto(
            video.Id,
            video.Title,
            video.Description,
            video.VideoUrl,
            video.ThumbnailUrl,
            video.DurationSeconds,
            video.ViewsCount,
            video.CreatedAt,
            new AuthorDto(video.Author.Id, video.Author.UserName ?? "", video.Author.DisplayName)
        ));
    }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)] 
    public async Task<IActionResult> UploadVideo([FromForm] UploadVideoRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("Video not selected");

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        var userId = Guid.Parse(userIdString);

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
            Status = VideoStatus.Processing
        };

        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        _queue.Enqueue(new VideoProcessingTask(video.Id, tempFilePath, request.File.FileName));

        return Accepted(new { id = video.Id, message = "Video recieved and processing" });
    }
}

public record UploadVideoRequest(string Title, string? Description, IFormFile File);
public record VideoDto(
    Guid Id,
    string Title,
    string? Description,
    string VideoUrl,
    string? ThumbnailUrl,
    double DurationSeconds,
    long ViewsCount,
    DateTime CreatedAt,
    AuthorDto Author
);