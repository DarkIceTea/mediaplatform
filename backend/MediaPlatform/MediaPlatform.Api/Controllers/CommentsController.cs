using System.Security.Claims;
using MediaPlatform.Core.Entities;
using MediaPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CommentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments([FromQuery] Guid postId)
    {
        var comments = await _context.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && c.ParentCommentId == null) 
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto(
                c.Id,
                c.Text,
                c.CreatedAt,
                new AuthorDto(c.Author.Id, c.Author.UserName ?? "", c.Author.DisplayName),
                c.ParentCommentId,
                c.Replies
                    .OrderBy(r => r.CreatedAt) 
                    .Select(r => new CommentDto(
                        r.Id,
                        r.Text,
                        r.CreatedAt,
                        new AuthorDto(r.Author.Id, r.Author.UserName ?? "", r.Author.DisplayName),
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
            return BadRequest("Comment  text is required");

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        var comment = new Comment
        {
            Text = request.Text.Trim(),
            PostId = request.PostId,
            ParentCommentId = request.ParentCommentId,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

        var dto = new CommentDto(
            comment.Id,
            comment.Text,
            comment.CreatedAt,
            new AuthorDto(comment.Author.Id, comment.Author.UserName ?? "", comment.Author.DisplayName),
            comment.ParentCommentId,
            new List<CommentDto>()
        );

        return Ok(dto);
    }
}

public record CreateCommentRequest(string Text, Guid? PostId, Guid? ParentCommentId);
public record CommentDto(Guid Id, string Text, DateTime CreatedAt, AuthorDto Author, Guid? ParentCommentId, List<CommentDto> Replies);