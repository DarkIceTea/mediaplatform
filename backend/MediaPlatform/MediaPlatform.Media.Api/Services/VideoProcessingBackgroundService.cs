using MediaPlatform.Media.Api.Data;
using MediaPlatform.Media.Api.Entities;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MediaPlatform.Media.Api.Services;

public class VideoProcessingBackgroundService : BackgroundService
{
    private readonly VideoProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoProcessingBackgroundService> _logger;

    public VideoProcessingBackgroundService(
        VideoProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VideoProcessingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
        if (!Directory.Exists(ffmpegPath) || !File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
        {
            _logger.LogInformation("Loading FFmpeg ...");
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
        }
        FFmpeg.SetExecutablesPath(ffmpegPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _queue.DequeueAsync(stoppingToken);
                await ProcessVideoAsync(task, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video processing error");
            }
        }
    }

    private async Task ProcessVideoAsync(VideoProcessingTask task, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var video = await db.Videos.FindAsync([task.VideoId], stoppingToken);
        if (video == null) return;

        var thumbnailTemp = Path.Combine(Path.GetTempPath(), $"{task.VideoId}_thumb.jpg");

        try
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(task.TempFilePath);
            video.DurationSeconds = mediaInfo.Duration.TotalSeconds;

            var snapshotTime = TimeSpan.FromSeconds(Math.Min(1, video.DurationSeconds));
            var conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(task.TempFilePath, thumbnailTemp, snapshotTime);
            await conversion.Start(stoppingToken);

            await using (var thumbStream = File.OpenRead(thumbnailTemp))
            {
                video.ThumbnailUrl = await storage.UploadFileAsync(thumbStream, $"{task.VideoId}.jpg", "image/jpeg");
            }

            await using (var videoStream = File.OpenRead(task.TempFilePath))
            {
                video.VideoUrl = await storage.UploadFileAsync(videoStream, task.OriginalFileName, "video/mp4");
            }

            video.Status = VideoStatus.Ready;
            _logger.LogInformation("Video ID {Id} successful processed!", video.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video process error ID {Id}", video.Id);
            video.Status = VideoStatus.Failed;
        }
        finally
        {
            if (File.Exists(task.TempFilePath)) File.Delete(task.TempFilePath);
            if (File.Exists(thumbnailTemp)) File.Delete(thumbnailTemp);
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}