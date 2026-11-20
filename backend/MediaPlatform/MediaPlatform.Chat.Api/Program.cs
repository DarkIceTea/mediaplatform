using System.Text;
using MediaPlatform.Chat.Api.Data;
using MediaPlatform.Chat.Api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ChatDbContext>("chatdb");

builder.Services.AddHttpClient("identity-service", client =>
{
    client.BaseAddress = new Uri("http://identity-api");
});

var redisConnection = builder.Configuration.GetConnectionString("redis");
var signalrBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(redisConnection))
{
    signalrBuilder.AddStackExchangeRedis(redisConnection);
}

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

if (args.Contains("--migrate"))
{
    Console.WriteLine("⏳ Start Migration chatdb...");
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("✅  Migrations chatdb successfully applied!");
    return;
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ChatDbContext>().Database.Migrate();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/hubs/chat");

app.Run();