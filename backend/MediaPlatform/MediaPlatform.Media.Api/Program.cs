using System.Text;
using MassTransit;
using MediaPlatform.Media.Api.Consumers;
using MediaPlatform.Media.Api.Data;
using MediaPlatform.Media.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 500 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 500 * 1024 * 1024);

builder.AddNpgsqlDbContext<MediaDbContext>("mediadb");

if (OperatingSystem.IsWindows())
{
    var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
    if (!Directory.Exists(ffmpegPath) || !File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
    {
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
    }
    FFmpeg.SetExecutablesPath(ffmpegPath);
}
else
{
    FFmpeg.SetExecutablesPath("/usr/bin");
}

builder.Services.AddScoped<IFileStorageService, MinioStorageService>();

builder.Services.AddMassTransit(x =>
{
  
    x.AddConsumer<ProcessVideoConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        cfg.Host(connectionString);

        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

        cfg.ConfigureEndpoints(context);
    });
});

var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.TryGetValue("jwt", out var token)) ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();


using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.Migrate();


app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();