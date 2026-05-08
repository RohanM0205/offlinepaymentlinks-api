using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;

namespace OfflinePaymentLinks.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/payment-summary")]
public class PaymentSummaryController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentSummaryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary([FromQuery] string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return BadRequest(new { message = "jobId is required." });

        var record = await _context.PrePaymentData
            .FirstOrDefaultAsync(p => p.JobRequestId == jobId);

        if (record == null)
            return NotFound(new { message = "Payment record not found." });

        // ── Check if already paid ──────────────────────────────────────────
        var postPayment = await _context.PostPaymentData
            .Where(p => p.JobRequestId == jobId && p.PaymentStatus == "Success")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (postPayment != null)
        {
            return Ok(new
            {
                status = "paid",
                invoiceNo = postPayment.InvoiceNo,
                refNo = postPayment.PaymentReferenceNo,
                txnNo = postPayment.PaymentTransactionNumber,
                amount = postPayment.Amount,
                paymentMode = postPayment.PaymentMode,
                paidAt = postPayment.PaymentSuccessAt,
            });
        }

        // ── Check URL expiry ───────────────────────────────────────────────
        if (record.UrlExpiration.HasValue && record.UrlExpiration < DateTime.Now)
        {
            return Ok(new
            {
                status = "expired",
                invoiceNo = record.InvoiceNo,
                expiredAt = record.UrlExpiration,
            });
        }

        // ── Valid — return full summary ────────────────────────────────────
        return Ok(new
        {
            status = "valid",
            email = record.EmailId,
            invoiceDate = record.CreatedDate,
            remarks = record.Remarks,
            amount = record.Amount,
            transactionType = record.TransactionType,
            product = record.Product,
            customerType = record.CustomerType,
            invoiceNo = record.InvoiceNo,
            jobRequestId = record.JobRequestId,
            paymentRefNo = record.PaymentReferenceNo,
            requestorMobile = record.RequestorMobile,
            policyNumber = record.PolicyNumber,
            channelType = record.ChannelType,
            paymentLink = record.PaymentLink,
        });
    }
}