using System.Security.Claims;
using MediaPlatform.Core.Entities;
using MediaPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PostsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 50) pageSize = 10;

        var totalCount = await _context.Posts.CountAsync();

        var posts = await _context.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostDto(
                p.Id,
                p.Title,
                p.Content,
                p.CoverImageUrl,
                p.CreatedAt,
                new AuthorDto(p.Author.Id, p.Author.UserName ?? "", p.Author.DisplayName)
            ))
            .ToListAsync();

        return Ok(new PagedResult<PostDto>(posts, totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var post = await _context.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.Id == id)
            .Select(p => new PostDto(
                p.Id,
                p.Title,
                p.Content,
                p.CoverImageUrl,
                p.CreatedAt,
                new AuthorDto(p.Author.Id, p.Author.UserName ?? "", p.Author.DisplayName)
            ))
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound("Post not found");

        return Ok(post);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();

        var userId = Guid.Parse(userIdString);

        var post = new Post
        {
            Title = request.Title,
            Content = request.Content,
            CoverImageUrl = request.CoverImageUrl,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, new { id = post.Id });
    }
}

public record CreatePostRequest(string Title, string Content, string? CoverImageUrl);
public record AuthorDto(Guid Id, string UserName, string DisplayName);
public record PostDto(Guid Id, string Title, string Content, string? CoverImageUrl, DateTime CreatedAt, AuthorDto Author);
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);