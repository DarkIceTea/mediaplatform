using MediaPlatform.Media.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaPlatform.Media.Api.Data;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }

    public DbSet<Video> Videos => Set<Video>();
}