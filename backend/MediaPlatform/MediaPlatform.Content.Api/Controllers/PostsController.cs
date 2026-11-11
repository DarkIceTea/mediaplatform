using System.Security.Claims;
using MediaPlatform.Content.Api.Data;
using MediaPlatform.Content.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Content.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ContentDbContext _context;

    public PostsController(ContentDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var total = await _context.Posts.CountAsync();
        var items = await _context.Posts
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostDto(
                p.Id, p.Title, p.Content, p.CoverImageUrl, p.CreatedAt,
                new AuthorDto(p.AuthorId, p.AuthorName, p.AuthorDisplayName)
            ))
            .ToListAsync();

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var post = await _context.Posts
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PostDto(
                p.Id, p.Title, p.Content, p.CoverImageUrl, p.CreatedAt,
                new AuthorDto(p.AuthorId, p.AuthorName, p.AuthorDisplayName)
            ))
            .FirstOrDefaultAsync();

        if (post == null) return NotFound("Статья не найдена");
        return Ok(post);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var displayName = User.FindFirstValue("displayName") ?? userName;

        var post = new Post
        {
            Title = request.Title,
            Content = request.Content,
            CoverImageUrl = request.CoverImageUrl,
            AuthorId = userId,
            AuthorName = userName,
            AuthorDisplayName = displayName
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, new { id = post.Id });
    }
}

public record CreatePostRequest(string Title, string Content, string? CoverImageUrl);
public record AuthorDto(Guid Id, string UserName, string DisplayName);
public record PostDto(Guid Id, string Title, string Content, string? CoverImageUrl, DateTime CreatedAt, AuthorDto Author);