using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfflinePaymentLinks.API.Models.DTOs;
using OfflinePaymentLinks.API.Services;

namespace OfflinePaymentLinks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var result = await _auth.LoginAsync(req);
            if (result is null)
                return Unauthorized(new { message = "Invalid email or password." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var (success, message) = await _auth.RegisterAsync(req);
        if (!success) return Conflict(new { message });
        return Ok(new { message });
    }

    [HttpGet("me"), Authorize]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        name = User.FindFirstValue("name"),
        role = User.FindFirstValue(ClaimTypes.Role),
    });
}