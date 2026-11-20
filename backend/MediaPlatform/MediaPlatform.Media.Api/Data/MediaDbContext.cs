using MediaPlatform.Media.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Media.Api.Data;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<VideoLike> VideoLikes => Set<VideoLike>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VideoLike>(entity =>
        {
            entity.HasKey(l => new { l.VideoId, l.UserId });

            entity.HasOne(l => l.Video)
                .WithMany(v => v.Likes)
                .HasForeignKey(l => l.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}