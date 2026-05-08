using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;
using OfflinePaymentLinks.API.Models;

namespace OfflinePaymentLinks.API.Controllers;

[Authorize]
[ApiController]
[Route("api/user-dashboard")]
public class UserDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const int PageSize = 10;

    public UserDashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);

        var links = await _context.PrePaymentData
            .Where(p => p.LinkSharedBy == email)
            .ToListAsync();

        var jobIds = links.Select(l => l.JobRequestId).ToList();

        var paidJobIds = (await _context.PostPaymentData
            .Where(p => p.PaymentStatus == "Success" && jobIds.Contains(p.JobRequestId))
            .Select(p => p.JobRequestId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var attemptedJobIds = (await _context.PostPaymentData
            .Where(p => p.PaymentStatus == "Failed" && jobIds.Contains(p.JobRequestId))
            .Select(p => p.JobRequestId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var now = DateTime.Now;

        var expiredJobIds = links
            .Where(l => l.UrlExpiration.HasValue && l.UrlExpiration < now)
            .Select(l => l.JobRequestId)
            .ToHashSet();

        var paid = paidJobIds.Count;
        var attempted = attemptedJobIds.Count;
        var pending = links.Count(l =>
            !paidJobIds.Contains(l.JobRequestId) &&
            !expiredJobIds.Contains(l.JobRequestId) &&
            !attemptedJobIds.Contains(l.JobRequestId));

        var totalAmount = await _context.PostPaymentData
            .Where(p => p.PaymentStatus == "Success" && paidJobIds.Contains(p.JobRequestId))
            .SumAsync(p => (decimal?)p.Amount ?? 0);

        return Ok(new
        {
            totalLinks = links.Count,
            paid,
            attempted,
            pending,
            expired = expiredJobIds.Count,
            totalAmount,
            thisMonth = links.Count(l => l.CreatedDate.HasValue &&
                            l.CreatedDate.Value.Month == now.Month &&
                            l.CreatedDate.Value.Year == now.Year),
        });
    }

    [HttpGet("links")]
    public async Task<IActionResult> GetLinks(
        [FromQuery] int page = 1,
        [FromQuery] string? transactionType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] string? sortBy = "date",
        [FromQuery] string? sortDir = "desc")
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var now = DateTime.Now;

        var query = _context.PrePaymentData
            .Where(p => p.LinkSharedBy == email)
            .AsQueryable();

        // ── Transaction type filter — map display name OR accept code directly ──
        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            var txMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "New Business", "NB"  }, { "NB",  "NB"  },
            { "Roll Over",    "RL"  }, { "RL",  "RL"  },
            { "Renewal",      "RN"  }, { "RN",  "RN"  },
            { "Endorsement",  "EN"  }, { "EN",  "EN"  },
            { "Shortfall",    "SF"  }, { "SF",  "SF"  },
            { "NCB Recovery", "NCB" }, { "NCB", "NCB" },
            { "2Rs Payment",  "2RS" }, { "2RS", "2RS" },
        };
            var code = txMap.TryGetValue(transactionType, out var mapped) ? mapped : transactionType;
            query = query.Where(p => p.TransactionType == code);
        }

        // ── Search ──────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                (p.Name != null && p.Name.Contains(search)) ||
                (p.PolicyNumber != null && p.PolicyNumber.Contains(search)) ||
                (p.InvoiceNo != null && p.InvoiceNo.Contains(search)) ||
                (p.EmailId != null && p.EmailId.Contains(search)));

        // ── Date range ──────────────────────────────────────────────────────────
        if (DateTime.TryParse(fromDate, out var from))
            query = query.Where(p => p.CreatedDate >= from);

        if (DateTime.TryParse(toDate, out var to))
            query = query.Where(p => p.CreatedDate <= to.AddDays(1));

        var links = await query.ToListAsync();

        // ── Enrich with payment status ──────────────────────────────────────────
        var jobIds = links.Select(l => l.JobRequestId).ToList();

        var paidJobIds = (await _context.PostPaymentData
            .Where(p => p.PaymentStatus == "Success" && jobIds.Contains(p.JobRequestId))
            .Select(p => p.JobRequestId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var attemptedJobIds = (await _context.PostPaymentData
            .Where(p => p.PaymentStatus == "Failed" && jobIds.Contains(p.JobRequestId))
            .Select(p => p.JobRequestId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var result = links.Select(l =>
        {
            var isPaid = paidJobIds.Contains(l.JobRequestId);
            var isExpired = l.UrlExpiration.HasValue && l.UrlExpiration < now;
            var linkStatus = isPaid ? "Paid"
                : isExpired ? "Expired"
                : attemptedJobIds.Contains(l.JobRequestId) ? "Attempted"
                : "Pending";

            return new
            {
                l.JobRequestId,
                l.InvoiceNo,
                l.PaymentReferenceNo,
                l.Name,
                l.EmailId,
                l.MobileNumber,
                l.PolicyNumber,
                l.TransactionType,
                l.Product,
                l.Amount,
                l.ShortUrl,
                l.UrlExpiration,
                l.CreatedDate,
                Status = linkStatus,
            };
        }).AsQueryable();

        // ── Status filter (applied after enrichment) ────────────────────────────
        if (!string.IsNullOrWhiteSpace(status))
            result = result.Where(r => r.Status.ToLower() == status.ToLower());

        // ── Sort ────────────────────────────────────────────────────────────────
        result = (sortBy?.ToLower(), sortDir?.ToLower()) switch
        {
            ("amount", "asc") => result.OrderBy(r => r.Amount),
            ("amount", "desc") => result.OrderByDescending(r => r.Amount),
            ("status", _) => result.OrderBy(r => r.Status),
            (_, "asc") => result.OrderBy(r => r.CreatedDate),
            _ => result.OrderByDescending(r => r.CreatedDate),
        };

        var total = result.Count();
        var paged = result.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        return Ok(new { items = paged, total, page, pageSize = PageSize });
    }

    // ── Recent Activity (last 8) ──────────────────────────────────────────
    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var now = DateTime.Now;

        var recent = await _context.PrePaymentData
            .Where(p => p.LinkSharedBy == email)
            .OrderByDescending(p => p.CreatedDate)
            .Take(8)
            .ToListAsync();

        var jobIds = recent.Select(r => r.JobRequestId).ToList();
        var paidIds = await _context.PostPaymentData
            .Where(p => p.PaymentStatus == "Success" && jobIds.Contains(p.JobRequestId))
            .Select(p => p.JobRequestId)
            .Distinct()
            .ToListAsync();

        var payments = await _context.PostPaymentData
            .Where(p => jobIds.Contains(p.JobRequestId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var activity = recent.Select(l =>
        {
            var isPaid = paidIds.Contains(l.JobRequestId);
            var isExpired = l.UrlExpiration.HasValue && l.UrlExpiration < now;
            var payment = payments.FirstOrDefault(p => p.JobRequestId == l.JobRequestId);

            return new
            {
                l.JobRequestId,
                l.InvoiceNo,
                l.Name,
                l.TransactionType,
                l.Product,
                l.Amount,
                l.ShortUrl,
                l.CreatedDate,
                Status = isPaid ? "Paid" : isExpired ? "Expired" : "Pending",
                PaymentMode = payment?.PaymentMode,
                PaidAt = payment?.PaymentSuccessAt,
            };
        });

        return Ok(activity);
    }

    // ── My Profile ────────────────────────────────────────────────────────
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var email = user.Email;

        var totalLinks = await _context.PrePaymentData
            .CountAsync(p => p.LinkSharedBy == email);

        return Ok(new
        {
            name = user.UserName,
            email = user.Email,
            role = roles.FirstOrDefault() ?? "—",
            registrationDate = user.RegistrationDate,
            approvedDate = user.Approveddate,
            approverName = user.ApproverName,
            totalLinks,
        });
    }
}