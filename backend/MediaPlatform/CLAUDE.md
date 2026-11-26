# Backend notes (backend/MediaPlatform)

## EF Core migrations — `--migrate` CLI
Every API `Program.cs` checks `args.Contains("--migrate")` right after `builder.Build()`: it resolves its DbContext, runs `MigrateAsync()` and exits without starting the web host. The k8s Jobs in `k8s/jobs/` use this. Startup auto-migration runs only when `IsDevelopment()`.
```bash
dotnet run --project MediaPlatform.Media.Api -- --migrate          # same flag on Identity/Content/Chat
dotnet ef migrations add <Name> --project MediaPlatform.Content.Api
```
Keep this pattern for any new service.

## Media.Api
- **MinIO** (`Services/MinioStorageService.cs`, AWS S3 SDK, `ForcePathStyle`, bucket `media-platform`): `Minio:Endpoint` is the address the service uses to talk to MinIO (in k8s: `http://minio:9000`); `Minio:PublicEndpoint` is the address used to build URLs the browser loads (in k8s: `http://localhost:9000`). Both default to `http://localhost:9000`. Never put the internal endpoint in URLs returned to clients.
- **FFmpeg** (`Program.cs`): on Windows, `Xabe.FFmpeg.Downloader` downloads binaries into `<BaseDirectory>/ffmpeg` on first run; elsewhere it uses `/usr/bin`, so Linux images must install `ffmpeg` there.
- **500 MB uploads** need the limit raised at every hop: Gateway Kestrel `MaxRequestBodySize`, Media.Api Kestrel `MaxRequestBodySize` + `FormOptions.MultipartBodyLengthLimit`, and `[RequestSizeLimit]` on `VideosController` upload. Change all of them together.
- **MassTransit 8.\*** with two transports, chosen by the `messaging` connection string:
  - RabbitMQ (default/local): `ConfigureEndpoints(context)`, retry 3 × 5s.
  - Azure Service Bus **Basic tier** (string contains `servicebus.windows.net`): Basic has queues only, no topics, so the receive endpoint `video-processing` sets `ConfigureConsumeTopology = false`, and the controller must **send** to `queue:video-processing` via `ISendEndpointProvider`. Don't switch to `Publish` (publishing needs topics).
  - Flow: upload → temp file → `VideoUploadedEvent` → `ProcessVideoConsumer` (thumbnail + duration via Xabe.FFmpeg) → status Processing → Ready/Failed.

## Gateway rate limiting
Policies are defined in `MediaPlatform.Gateway/Program.cs` and assigned per route via `RateLimiterPolicy` in `appsettings.json`, partitioned by client IP and returning 429 with a JSON body:
- `auth-policy`: sliding window, 5 req/min (auth routes)
- `upload-policy`: fixed window, 3 req/min (uploads)
- `general-policy`: token bucket (everything else)

## Content.Api — Redis cache-aside
`builder.AddRedisDistributedCache("redis")` + `Common/DistributedCacheExtensions.cs` (`GetRecordAsync` / `SetRecordAsync`, default 5 min absolute TTL). Reads check the cache, then fall back to the DB and populate the cache; writes invalidate keys. Feed keys look like `posts_feed_p{page}_s{size}` and single posts `post_{id}`. The feed is cached for 2 min. `CreatePost` only removes `posts_feed_p1_s10`, so other pages stay stale for up to 2 min.

## Chat.Api — SignalR with Redis backplane
`AddSignalR().AddStackExchangeRedis(...)` lets hub messages reach users connected to any replica. Hub `/hubs/chat`: `SendMessage(receiverId, content)` → `ReceiveMessage` (receiver) / `MessageSent` (sender). The user list comes from Identity.Api over HTTP (`http://identity-api`).
