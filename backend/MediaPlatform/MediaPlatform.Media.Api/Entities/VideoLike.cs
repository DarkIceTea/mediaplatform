namespace MediaPlatform.Media.Api.Entities;

public class VideoLike
{
    public Guid VideoId { get; set; }
    public Video Video { get; set; } = null!;

    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}