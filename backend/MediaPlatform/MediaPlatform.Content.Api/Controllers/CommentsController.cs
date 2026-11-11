using System.Security.Claims;
using MediaPlatform.Content.Api.Data;
using MediaPlatform.Content.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Content.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ContentDbContext _context;

    public CommentsController(ContentDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments([FromQuery] Guid? postId, [FromQuery] Guid? videoId)
    {
        var query = _context.Comments.AsNoTracking().Where(c => c.ParentCommentId == null);

        if (postId.HasValue)
            query = query.Where(c => c.PostId == postId.Value);
        else if (videoId.HasValue)
            query = query.Where(c => c.VideoId == videoId.Value);
        else
            return BadRequest("Не указан postId или videoId");

        var comments = await query
            .Include(c => c.Replies)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id, c.Text, c.CreatedAt,
                new AuthorDto(c.AuthorId, c.AuthorName, c.AuthorDisplayName),
                c.ParentCommentId,
                c.Replies.OrderBy(r => r.CreatedAt).Select(r => new CommentDto(
                    r.Id, r.Text, r.CreatedAt,
                    new AuthorDto(r.AuthorId, r.AuthorName, r.AuthorDisplayName),
                    r.ParentCommentId,
                    new List<CommentDto>()
                )).ToList()
            ))
            .ToListAsync();

        return Ok(comments);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Текст комментария не может быть пустым");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var displayName = User.FindFirstValue("displayName") ?? userName;

        var comment = new Comment
        {
            Text = request.Text.Trim(),
            PostId = request.PostId,
            VideoId = request.VideoId,
            ParentCommentId = request.ParentCommentId,
            AuthorId = userId,
            AuthorName = userName,
            AuthorDisplayName = displayName
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var dto = new CommentDto(
            comment.Id, comment.Text, comment.CreatedAt,
            new AuthorDto(comment.AuthorId, comment.AuthorName, comment.AuthorDisplayName),
            comment.ParentCommentId,
            new List<CommentDto>()
        );

        return Ok(dto);
    }
}

public record CreateCommentRequest(string Text, Guid? PostId, Guid? VideoId, Guid? ParentCommentId);
public record CommentDto(Guid Id, string Text, DateTime CreatedAt, AuthorDto Author, Guid? ParentCommentId, List<CommentDto> Replies);