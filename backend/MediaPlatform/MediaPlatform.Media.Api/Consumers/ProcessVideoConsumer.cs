using MassTransit;
using MediaPlatform.Media.Api.Data;
using MediaPlatform.Media.Api.Entities;
using MediaPlatform.Media.Api.Messages;
using MediaPlatform.Media.Api.Services;
using Xabe.FFmpeg;

namespace MediaPlatform.Media.Api.Consumers;

public class ProcessVideoConsumer : IConsumer<VideoUploadedEvent>
{
    private readonly MediaDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ProcessVideoConsumer> _logger;

    public ProcessVideoConsumer(
        MediaDbContext dbContext,
        IFileStorageService fileStorage,
        ILogger<ProcessVideoConsumer> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VideoUploadedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("[RabbitMQ] Recieve task to process video: {VideoId}", message.VideoId);

        var video = await _dbContext.Videos.FindAsync(message.VideoId);
        if (video == null) return;

        var thumbnailTemp = Path.Combine(Path.GetTempPath(), $"{message.VideoId}_thumb.jpg");

        try
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(message.TempFilePath);
            video.DurationSeconds = mediaInfo.Duration.TotalSeconds;

            var snapshotTime = TimeSpan.FromSeconds(Math.Min(1, video.DurationSeconds));
            var conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(message.TempFilePath, thumbnailTemp, snapshotTime);
            await conversion.Start(context.CancellationToken);

            await using (var thumbStream = File.OpenRead(thumbnailTemp))
            {
                video.ThumbnailUrl = await _fileStorage.UploadFileAsync(thumbStream, $"{message.VideoId}.jpg", "image/jpeg");
            }

            await using (var videoStream = File.OpenRead(message.TempFilePath))
            {
                video.VideoUrl = await _fileStorage.UploadFileAsync(videoStream, message.OriginalFileName, "video/mp4");
            }

            video.Status = VideoStatus.Ready;
            _logger.LogInformation("✅ [RabbitMQ] Video {VideoId} successful loaded!", video.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RabbitMQ] Error video processing {VideoId}", video.Id);
            video.Status = VideoStatus.Failed;
            throw; 
        }
        finally
        {
            if (File.Exists(message.TempFilePath)) File.Delete(message.TempFilePath);
            if (File.Exists(thumbnailTemp)) File.Delete(thumbnailTemp);

            await _dbContext.SaveChangesAsync();
        }
    }
}