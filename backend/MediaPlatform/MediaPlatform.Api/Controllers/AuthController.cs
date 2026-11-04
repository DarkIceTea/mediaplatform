using System.Security.Claims;
using MediaPlatform.Core.Entities;
using MediaPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediaPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(UserManager<User> userManager, JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest("User with this email already exists");

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.UserName, user.Email, user.DisplayName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized("Wrong password or email");

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.UserName ?? "", user.Email ?? "", user.DisplayName));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName,
            user.CreatedAt
        });
    }
}

public record RegisterRequest(string UserName, string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string UserName, string Email, string DisplayName);