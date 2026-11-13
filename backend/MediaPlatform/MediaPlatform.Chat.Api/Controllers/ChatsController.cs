using System.Security.Claims;
using MediaPlatform.Chat.Api.Data;
using MediaPlatform.Chat.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Chat.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly ChatDbContext _context;
    private readonly IHttpClientFactory _clientFactory;

    public ChatsController(ChatDbContext context, IHttpClientFactory clientFactory)
    {
        _context = context;
        _clientFactory = clientFactory;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var client = _clientFactory.CreateClient("identity-service");
        var users = await client.GetFromJsonAsync<List<ChatUserDto>>("/api/auth/users");

        var otherUsers = users?.Where(u => u.Id.ToString() != currentUserId).ToList();
        return Ok(otherUsers ?? new List<ChatUserDto>());
    }

    [HttpGet("{otherUserId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid otherUserId)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                        (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SentAt)
            .Take(100)
            .Select(m => new ChatMessageDto(
                m.Id, m.SenderId, m.SenderName, m.ReceiverId, m.Content, m.SentAt
            ))
            .ToListAsync();

        return Ok(messages);
    }
}

public record ChatUserDto(Guid Id, string UserName, string DisplayName);