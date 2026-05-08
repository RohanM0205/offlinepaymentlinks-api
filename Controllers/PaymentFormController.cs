using System.Security.Claims;
using System.Text.Json;
using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;
using OfflinePaymentLinks.API.Helpers;
using OfflinePaymentLinks.API.Models;
using OfflinePaymentLinks.API.Services;

namespace OfflinePaymentLinks.API.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public class PaymentFormController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GenericPaymentsFetchService _service;
    private readonly ApplicationDbContext _context;
    private readonly PaymentUtilityService _paymentUtilityService;

    public PaymentFormController(
        UserManager<ApplicationUser> userManager,
        GenericPaymentsFetchService service,
        ApplicationDbContext context,
        PaymentUtilityService paymentUtilityService)
    {
        _userManager = userManager;
        _service = service;
        _context = context;
        _paymentUtilityService = paymentUtilityService;
    }

    // ── Permissions ───────────────────────────────────────────────────────

    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roles.FirstOrDefault());
        if (role == null)
            return Ok(new { canSendPaymentLink = false, allowedTransactionTypes = new List<string>() });

        var perm = await _context.RolePermissions
            .FirstOrDefaultAsync(p => p.RoleId == role.Id);

        if (perm == null || !perm.CanSendPaymentLink)
            return Ok(new { canSendPaymentLink = false, allowedTransactionTypes = new List<string>() });

        var allowed = JsonSerializer.Deserialize<List<string>>(perm.AllowedTransactionTypes) ?? new List<string>();
        return Ok(new { canSendPaymentLink = true, allowedTransactionTypes = allowed });
    }

    // ── KYC ───────────────────────────────────────────────────────────────

    [HttpGet("kyc/{kycId}")]
    public IActionResult GetKycDetails(string kycId)
    {
        var kycData = _service.FetchKYC(kycId);
        if (kycData == null)
            return NotFound(new { message = "KYC ID not found" });

        if (kycData.KYC_Status?.ToUpper() == "REJECTED")
            return BadRequest(new
            {
                code = "KYC_REJECTED",
                message = "KYC ID status is rejected. Kindly ask the customer to create a new KYC ID."
            });

        return Ok(kycData);
    }

    [HttpGet("kyc-by-pan-dob")]
    public IActionResult GetKycByPanDob([FromQuery] string pan, [FromQuery] string dob)
    {
        if (string.IsNullOrWhiteSpace(pan) || string.IsNullOrWhiteSpace(dob))
            return BadRequest(new { message = "PAN and DOB are required." });

        if (!DateTime.TryParse(dob, out var dobDate))
            return BadRequest(new { message = "Invalid date format." });

        var data = _context.KYC_Information
            .FirstOrDefault(k =>
                k.PAN_Number == pan.ToUpper().Trim() &&
                k.DOB.HasValue && k.DOB.Value.Date == dobDate.Date);

        return data == null
            ? NotFound(new { message = "No KYC record found for given PAN and DOB." })
            : Ok(data);
    }

    // ── Policy ────────────────────────────────────────────────────────────

    [HttpGet("policy/{policyNumber}")]
    public IActionResult GetPolicyDetails(string policyNumber)
    {
        var result = _service.GetPolicyDetails(policyNumber);
        return result == null
            ? NotFound(new { message = "Policy record not found." })
            : Ok(result);
    }

    [HttpGet("shortfall-search")]
    public IActionResult ShortFallSearch(
        [FromQuery] string? inwardNumber,
        [FromQuery] string? customerId,
        [FromQuery] string? interactionId)
    {
        var paramCount = new[] { inwardNumber, customerId, interactionId }
            .Count(p => !string.IsNullOrWhiteSpace(p));

        if (paramCount != 1)
            return BadRequest(new { message = "Provide exactly one search parameter." });

        var result = _service.ShortFallSearch(inwardNumber, customerId, interactionId);
        return result == null
            ? NotFound(new { message = "No record found." })
            : Ok(result);
    }

    // ── Pincode ───────────────────────────────────────────────────────────

    [HttpGet("pincode/{pinCode}")]
    public IActionResult GetLocationByPinCode(string pinCode)
    {
        if (string.IsNullOrWhiteSpace(pinCode) || pinCode.Length != 6 || !pinCode.All(char.IsDigit))
            return BadRequest(new { message = "Please provide a valid 6-digit pin code." });

        var result = _service.GetPinCodeInformation(pinCode);
        return result == null
            ? NotFound(new { message = "Pin code not found." })
            : Ok(new { result.Locality, result.City, result.State });
    }

    // ── Submit ────────────────────────────────────────────────────────────

    [HttpPost("process-and-send")]
    public async Task<IActionResult> ProcessAndSend([FromBody] ProcessAndSendRequest request)
    {
        if (request?.Data == null)
            return BadRequest(new { message = "Invalid request." });

        var agentEmail = User.FindFirstValue(ClaimTypes.Email);

        // Generate IDs
        var ids = _paymentUtilityService.GenerateUniquePaymentIds();

        // Generate long URL
        string baseUrl = "http://localhost:5173/OfflinePaymentsClicks";
        string endpoint = (request.Data.TransactionType == "NB" || request.Data.TransactionType == "RL")
            ? "PaymentSummary"
            : "KYCDataCompare";
        string longUrl = $"{baseUrl}/{endpoint}?jobId={ids.JobRequestId}";

        // Shorten URL
        string shortCode;
        do { shortCode = ShortCodeGenerator.Generate(); }
        while (await _context.UrlMappings.AnyAsync(u => u.ShortCode == shortCode));

        var shortUrl = $"http://localhost:5173/r/{shortCode}";

        _context.UrlMappings.Add(new UrlMapping
        {
            OriginalUrl = longUrl,
            ShortCode = shortCode,
            ShortUrl = shortUrl,
            ExpiryDate = DateTime.Now.AddHours(24),  // ← changed
        });

        // Save PrePaymentData
        var data = request.Data;
        data.JobRequestId = ids.JobRequestId;
        data.InvoiceNo = ids.InvoiceNo;
        data.PaymentReferenceNo = ids.PaymentReferenceNo;
        data.CreatedDate = DateTime.Now;
        data.LastUpdated = DateTime.Now;
        data.PaymentLink = longUrl;
        data.ShortUrl = shortUrl;
        data.UrlExpiration = DateTime.Now.AddHours(24);  // ← changed

        var emailsFromRequest = request.Data.RequestorEmails;
        var validEmails = emailsFromRequest?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        data.RequestorEmails = validEmails != null && validEmails.Any()
            ? string.Join(",", validEmails)
            : agentEmail;

        data.LinkSharedBy = agentEmail;
        data.ProductType = request.Data.ProductType;
        data.ProductCode = request.Data.ProductCode;

        _context.PrePaymentData.Add(data);
        await _context.SaveChangesAsync();

        if (request.SendEmail) { /* TODO */ }
        if (request.SendSms) { /* TODO */ }

        return Ok(new
        {
            message = "Payment link generated successfully.",
            shortUrl,
            longUrl,
            jobRequestId = ids.JobRequestId,
            invoiceNo = ids.InvoiceNo,
            paymentReferenceNo = ids.PaymentReferenceNo,
        });
    }

    [HttpPost("upload-files")]
    public async Task<IActionResult> UploadAndZipFiles(
        [FromForm] string invoiceNumber,
        [FromForm] List<IFormFile> files)
    {
        if (string.IsNullOrEmpty(invoiceNumber))
            return BadRequest(new { message = "Invoice number is required." });
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files uploaded." });
        if (files.Count > 4)
            return BadRequest(new { message = "Maximum 4 files allowed." });

        string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
        string folderPath = Path.Combine(rootPath, invoiceNumber);
        Directory.CreateDirectory(folderPath);

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file.FileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        string zipPath = Path.Combine(rootPath, $"{invoiceNumber}.zip");
        if (System.IO.File.Exists(zipPath)) System.IO.File.Delete(zipPath);
        ZipFile.CreateFromDirectory(folderPath, zipPath);
        Directory.Delete(folderPath, true);

        return Ok(new { zipPath });
    }

    [HttpGet("products")]
    public IActionResult GetProducts()
    {
        var products = _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductType)
            .ThenBy(p => p.SortOrder)
            .Select(p => new {
                p.Id,
                p.ProductType,
                p.ProductName,
                p.ProductCode,
            })
            .ToList();

        // Group by ProductType
        var grouped = products
            .GroupBy(p => p.ProductType)
            .Select(g => new {
                productType = g.Key,
                products = g.Select(p => new {
                    p.ProductName,
                    p.ProductCode,
                }).ToList()
            })
            .ToList();

        return Ok(grouped);
    }
}

public class ProcessAndSendRequest
{
    public PrePaymentData Data { get; set; } = null!;
    public bool SendEmail { get; set; }
    public bool SendSms { get; set; }
}