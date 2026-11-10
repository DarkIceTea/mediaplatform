using MediaPlatform.Core.Entities;
using MediaPlatform.Core.Interfaces;
using MediaPlatform.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MediaPlatform.Infrastructure.Services;

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
            _logger.LogInformation("Loading ffmpeg...");
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
        }
        FFmpeg.SetExecutablesPath(ffmpegPath);

        _logger.LogInformation("Video processing background service started!");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _queue.DequeueAsync(stoppingToken);
                await ProcessVideoAsync(task, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing video processing background service!");
            }
        }
    }

    private async Task ProcessVideoAsync(VideoProcessingTask task, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var video = await dbContext.Videos.FindAsync([task.VideoId], stoppingToken);
        if (video == null) return;

        var thumbnailTempPath = Path.Combine(Path.GetTempPath(), $"{task.VideoId}_thumb.jpg");

        try
        {
            _logger.LogInformation("Start processing video id: {Id}", video.Id);

            var mediaInfo = await FFmpeg.GetMediaInfo(task.TempFilePath);
            video.DurationSeconds = mediaInfo.Duration.TotalSeconds;

            var snapshotTime = TimeSpan.FromSeconds(Math.Min(1, video.DurationSeconds));
            var conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(
                task.TempFilePath,
                thumbnailTempPath,
                snapshotTime
            );
            await conversion.Start(stoppingToken);

            await using (var thumbStream = File.OpenRead(thumbnailTempPath))
            {
                var thumbUrl = await fileStorage.UploadFileAsync(thumbStream, $"{task.VideoId}.jpg", "image/jpeg");
                video.ThumbnailUrl = thumbUrl;
            }

            await using (var videoStream = File.OpenRead(task.TempFilePath))
            {
                var videoUrl = await fileStorage.UploadFileAsync(videoStream, task.OriginalFileName, "video/mp4");
                video.VideoUrl = videoUrl;
            }

            video.Status = VideoStatus.Ready;
            _logger.LogInformation("Video ID: {Id} successfully processed", video.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error video processing ID: {Id}", video.Id);
            video.Status = VideoStatus.Failed;
        }
        finally
        {
            if (File.Exists(task.TempFilePath)) File.Delete(task.TempFilePath);
            if (File.Exists(thumbnailTempPath)) File.Delete(thumbnailTempPath);

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}