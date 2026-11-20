using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; 
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; 
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\": \"Too many requests. Please wait a minute.\", \"status\": 429}",
            cancellationToken: token);
    };

    options.AddPolicy("auth-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6, 
            QueueLimit = 0
        });
    });

    options.AddPolicy("upload-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    
    options.AddPolicy("general-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetTokenBucketLimiter(clientIp, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 60,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            QueueLimit = 0
        });
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();



app.UseCors("AllowAngular");

app.UseRateLimiter();

app.MapReverseProxy();

app.Run();