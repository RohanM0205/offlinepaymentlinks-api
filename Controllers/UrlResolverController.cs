using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;

namespace OfflinePaymentLinks.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/resolve")]
public class UrlResolverController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UrlResolverController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> Resolve(string shortCode)
    {
        var mapping = await _context.UrlMappings
            .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

        if (mapping == null)
            return NotFound(new { status = "not_found", message = "This link does not exist." });

        if (mapping.ExpiryDate <= DateTime.Now)
            return Ok(new { status = "expired", message = "This payment link has expired." });

        return Ok(new { status = "valid", redirectUrl = mapping.OriginalUrl });
    }
}