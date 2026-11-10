using System.Threading.Channels;

namespace MediaPlatform.Infrastructure.Services;

public record VideoProcessingTask(Guid VideoId, string TempFilePath, string OriginalFileName);

public class VideoProcessingQueue
{
    private readonly Channel<VideoProcessingTask> _queue = Channel.CreateUnbounded<VideoProcessingTask>();

    public void Enqueue(VideoProcessingTask task)
    {
        _queue.Writer.TryWrite(task);
    }

    public ValueTask<VideoProcessingTask> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}