using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfflinePaymentLinks.API.Models;
using OfflinePaymentLinks.API.Models.DTOs;

namespace OfflinePaymentLinks.API.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest req);
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest req);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _cfg;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration cfg)
    {
        _userManager = userManager;
        _cfg = cfg;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, req.Password))
            return null;

        if (!user.IsApproved)
            throw new InvalidOperationException("Your account is pending approval by an administrator.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Agent";
        var token = GenerateToken(user, role);

        return new AuthResponse(
    token,
    new UserDto(
        user.Id,
        user.Email!,
        user.UserName ?? user.Email!.Split('@')[0],  // ← add fallback
        role,
        user.IsApproved
    )
);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest req)
    {
        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing != null)
            return (false, "An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = req.Name,
            Email = req.Email,
            EmailConfirmed = true,
            IsApproved = false,
            RegistrationDate = DateTime.Now,
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        // New self-registrations get no role until approved
        return (true, "Registration successful. Your account is awaiting approval.");
    }

    private string GenerateToken(ApplicationUser user, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("name", user.UserName ?? user.Email!.Split('@')[0]),  // ← add fallback
            new Claim(ClaimTypes.Role,                role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}