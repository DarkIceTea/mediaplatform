using System.Security.Claims;
using MediaPlatform.Chat.Api.Data;
using MediaPlatform.Chat.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MediaPlatform.Chat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatDbContext _context;

    public ChatHub(ChatDbContext context)
    {
        _context = context;
    }

    public async Task SendMessage(Guid receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var senderId = Guid.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var senderName = Context.User?.FindFirstValue("displayName") 
                         ?? Context.User?.FindFirstValue(ClaimTypes.Name) 
                         ?? "Anonymous";

        var message = new ChatMessage
        {
            SenderId = senderId,
            SenderName = senderName,
            ReceiverId = receiverId,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var messageDto = new ChatMessageDto(
            message.Id,
            message.SenderId,
            message.SenderName,
            message.ReceiverId,
            message.Content,
            message.SentAt
        );

        await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", messageDto);
        await Clients.Caller.SendAsync("MessageSent", messageDto);
    }
}

public record ChatMessageDto(Guid Id, Guid SenderId, string SenderName, Guid ReceiverId, string Content, DateTime SentAt);