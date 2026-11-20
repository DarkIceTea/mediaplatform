namespace MediaPlatform.Media.Api.Messages;

public record VideoUploadedEvent(Guid VideoId, string TempFilePath, string OriginalFileName);