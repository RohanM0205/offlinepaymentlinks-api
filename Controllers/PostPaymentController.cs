using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;
using OfflinePaymentLinks.API.Models;

namespace OfflinePaymentLinks.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/post-payment")]
public class PostPaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PostPaymentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Record payment result (called from success/failure page) ──────────
    [HttpPost("record")]
    public async Task<IActionResult> Record([FromBody] RecordPaymentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobRequestId))
            return BadRequest(new { message = "JobRequestId is required." });

        // Pull base data from PrePaymentData
        var pre = await _context.PrePaymentData
            .FirstOrDefaultAsync(p => p.JobRequestId == req.JobRequestId);

        var entry = new PostPaymentData
        {
            JobRequestId = req.JobRequestId,
            InvoiceNo = pre?.InvoiceNo,
            PaymentReferenceNo = pre?.PaymentReferenceNo,
            CustomerName = pre?.Name,
            CustomerEmail = pre?.EmailId,
            CustomerMobile = pre?.MobileNumber,
            PolicyNumber = pre?.PolicyNumber,
            TransactionType = pre?.TransactionType,
            Product = pre?.Product,
            ProductType = pre?.ProductType,
            ProductCode = pre?.ProductCode,
            Amount = pre?.Amount,
            ChannelType = pre?.ChannelType,
            CustomerType = pre?.CustomerType,
            RequestorEmail = pre?.RequestorEmails,
            LinkSharedBy = pre?.LinkSharedBy,
            Remarks = pre?.Remarks,

            // Payment specific — from request
            PaymentMode = req.PaymentMode,
            PaymentTransactionNumber = req.PaymentTransactionNumber,
            PaymentId = req.PaymentId,
            PaymentStatus = req.PaymentStatus,
            FailureReason = req.FailureReason,
            FailureCode = req.FailureCode,
            PaymentAttemptedAt = req.PaymentAttemptedAt,
            PaymentSuccessAt = req.PaymentStatus == "Success" ? DateTime.Now : null,

            // Instrument details
            UpiId = req.UpiId,
            CardLast4 = req.CardLast4,
            CardNetwork = req.CardNetwork,
            CardType = req.CardType,
            BankName = req.BankName,
            WalletName = req.WalletName,

            // Meta
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            CreatedAt = DateTime.Now,
            LastUpdated = DateTime.Now,
        };

        _context.PostPaymentData.Add(entry);

        // Also update PrePaymentData payment status
        if (pre != null)
        {
            pre.LastUpdated = DateTime.Now;
            // You can add a PaymentStatus column to PrePaymentData if needed
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment recorded successfully.",
            id = entry.Id,
        });
    }

    // ── Get payment record by jobId ────────────────────────────────────────
    [HttpGet("{jobRequestId}")]
    public async Task<IActionResult> Get(string jobRequestId)
    {
        var entry = await _context.PostPaymentData
            .Where(p => p.JobRequestId == jobRequestId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (entry == null)
            return NotFound(new { message = "No payment record found." });

        return Ok(entry);
    }
}

public class RecordPaymentRequest
{
    public string JobRequestId { get; set; } = "";
    public string? PaymentMode { get; set; }
    public string? PaymentTransactionNumber { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentStatus { get; set; }
    public string? FailureReason { get; set; }
    public string? FailureCode { get; set; }
    public DateTime? PaymentAttemptedAt { get; set; }
    public string? UpiId { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardNetwork { get; set; }
    public string? CardType { get; set; }
    public string? BankName { get; set; }
    public string? WalletName { get; set; }
}