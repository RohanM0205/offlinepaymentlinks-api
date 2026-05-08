using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfflinePaymentLinks.API.Data;
using OfflinePaymentLinks.API.Models;
using OfflinePaymentLinks.API.Services;

namespace OfflinePaymentLinks.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/kyc-verify")]
public class KycVerifyController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly GenericPaymentsFetchService _fetchService;

    public KycVerifyController(
        ApplicationDbContext context,
        GenericPaymentsFetchService fetchService)
    {
        _context = context;
        _fetchService = fetchService;
    }

    // ── Get proposal details by jobId (public) ────────────────────────────
    [HttpGet("proposal")]
    public async Task<IActionResult> GetProposal([FromQuery] string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return BadRequest(new { message = "jobId is required." });

        var record = await _context.PrePaymentData
            .FirstOrDefaultAsync(p => p.JobRequestId == jobId);

        if (record == null)
            return NotFound(new { message = "No record found for this link." });

        return Ok(new
        {
            jobRequestId = record.JobRequestId,
            //nameAsPerProposal = MaskName(record.Name),
            nameAsPerProposal = record.Name,
            transactionType = record.TransactionType,
        });
    }

    // ── Verify KYC ID and run name match ──────────────────────────────────
    [HttpPost("verify-kyc")]
    public async Task<IActionResult> VerifyKyc([FromBody] VerifyKycRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId) || string.IsNullOrWhiteSpace(req.KycId))
            return BadRequest(new { message = "JobId and KycId are required." });

        // Fetch proposal
        var proposal = await _context.PrePaymentData
            .FirstOrDefaultAsync(p => p.JobRequestId == req.JobId);

        if (proposal == null)
            return NotFound(new { message = "Proposal not found." });

        // Fetch KYC
        var kyc = _fetchService.FetchKYC(req.KycId);
        if (kyc == null)
            return NotFound(new { message = "KYC ID not found." });

        // ── Check KYC status before name match ──────
        if (kyc.KYC_Status?.ToUpper() == "REJECTED")
            return BadRequest(new
            {
                code = "KYC_REJECTED",
                message = "Your KYC ID is rejected. Kindly create a new KYC ID and retry."
            });

        // Name match
        var proposalName = proposal.Name ?? "";
        var kycName = kyc.Name ?? "";
        var percentage = NameMatchService.GetMatchPercentage(proposalName, kycName);
        var status = NameMatchService.GetMatchStatus(percentage);

        // Store / update NameMatchLog
        var existing = await _context.NameMatchLogs
            .FirstOrDefaultAsync(n => n.JobRequestId == req.JobId);

        if (existing != null)
        {
            existing.NameAsPerKyc = kycName;
            existing.MatchPercentage = percentage;
            existing.MatchStatus = status;
            existing.KycId = req.KycId;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.NameMatchLogs.Add(new NameMatchLog
            {
                JobRequestId = req.JobId,
                NameAsPerProposal = proposalName,
                NameAsPerKyc = kycName,
                MatchPercentage = percentage,
                MatchStatus = status,
                KycId = req.KycId,
            });
        }

        // Always update Name_Mismatch_Status in PrePaymentData immediately
        proposal.Name_Mismatch_Status = status;
        proposal.LastUpdated = DateTime.Now;

        await _context.SaveChangesAsync();

        if (status == "Rejected")
            return Ok(new
            {
                status = "Rejected",
                matchPercentage = percentage,
                message = "Name verification failed. The name in your KYC does not match our records.",
            });

        return Ok(new
        {
            status = "Approved",
            matchPercentage = percentage,
            kycDetails = new
            {
                name = kyc.Name,
                mobile = kyc.Mobile,
                email = kyc.Email,
                address = $"{kyc.Address1} {kyc.Address2}".Trim(),
                pincode = kyc.Pin_Code,
                city = kyc.City,
                state = kyc.State,
                kycStatus = kyc.KYC_Status,
            }
        });
    }

    // ── Submit ────────────────────────────────────────────────────────────
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] KycSubmitRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.JobId))
            return BadRequest(new { message = "JobId is required." });

        var log = await _context.NameMatchLogs
            .FirstOrDefaultAsync(n => n.JobRequestId == req.JobId);

        if (log == null || log.MatchStatus != "Approved")
            return BadRequest(new { message = "KYC verification not completed or not approved." });

        var proposal = await _context.PrePaymentData
            .FirstOrDefaultAsync(p => p.JobRequestId == req.JobId);

        if (proposal != null)
        {
            proposal.KYC_ID = log.KycId;
            proposal.Name_Mismatch_Status = log.MatchStatus;
            proposal.KYC_Name = log.NameAsPerKyc;
            proposal.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            redirectUrl = $"/payment-summary?jobId={req.JobId}"
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string MaskName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "—";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts.Select(p =>
            p.Length <= 2 ? p : p[0] + new string('*', p.Length - 2) + p[^1]
        ));
    }
}

public record VerifyKycRequest(string JobId, string KycId);
public record KycSubmitRequest(string JobId);