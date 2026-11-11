using System.Security.Claims;
using MediaPlatform.Content.Api.Common;
using MediaPlatform.Content.Api.Data;
using MediaPlatform.Content.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace MediaPlatform.Content.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ContentDbContext _context;
    private readonly IDistributedCache _cache;

    public PostsController(ContentDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var cacheKey = $"posts_feed_p{page}_s{pageSize}";

        var cachedData = await _cache.GetRecordAsync<PagedPostsResponse>(cacheKey);
        if (cachedData != null)
        {
            return Ok(cachedData);
        }
        
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

        var response = new PagedPostsResponse(items, total, page, pageSize);

        await _cache.SetRecordAsync(cacheKey, response, TimeSpan.FromMinutes(2));

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var cacheKey = $"post_{id}";

        var cachedPost = await _cache.GetRecordAsync<PostDto>(cacheKey);
        if (cachedPost != null)
        {
            return Ok(cachedPost);
        }

        var post = await _context.Posts
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PostDto(
                p.Id, p.Title, p.Content, p.CoverImageUrl, p.CreatedAt,
                new AuthorDto(p.AuthorId, p.AuthorName, p.AuthorDisplayName)
            ))
            .FirstOrDefaultAsync();

        if (post == null) return NotFound("Post Not  Found");

        await _cache.SetRecordAsync(cacheKey, post, TimeSpan.FromMinutes(10));

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

        await _cache.RemoveAsync("posts_feed_p1_s10");

        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, new { id = post.Id });
    }
}

public record PagedPostsResponse(List<PostDto> Items, int TotalCount, int Page, int PageSize);
public record CreatePostRequest(string Title, string Content, string? CoverImageUrl);
public record AuthorDto(Guid Id, string UserName, string DisplayName);
public record PostDto(Guid Id, string Title, string Content, string? CoverImageUrl, DateTime CreatedAt, AuthorDto Author);