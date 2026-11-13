using MediaPlatform.Chat.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Chat.Api.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
}