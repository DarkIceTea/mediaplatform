namespace MediaPlatform.Media.Api.Entities;

public enum VideoStatus
{
    Processing = 0,
    Ready = 1,
    Failed = 2
}

public class Video
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public double DurationSeconds { get; set; }
    public long ViewsCount { get; set; } = 0;
    public VideoStatus Status { get; set; } = VideoStatus.Processing;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
}