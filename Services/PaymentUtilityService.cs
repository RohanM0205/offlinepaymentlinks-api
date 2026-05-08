using OfflinePaymentLinks.API.Data;

namespace OfflinePaymentLinks.API.Services;

public class PaymentUtilityService
{
    private readonly ApplicationDbContext _context;
    private readonly Random _random;

    public PaymentUtilityService(ApplicationDbContext context)
    {
        _context = context;
        _random = new Random();
    }

    public (string JobRequestId, string PaymentReferenceNo, string InvoiceNo) GenerateUniquePaymentIds()
    {
        string datePart = DateTime.UtcNow.ToString("yyyyMMdd");

        int jobReqCounter = _context.PrePaymentData.Count(p => p.JobRequestId != null) + 1;
        int paymentRefCounter = _context.PrePaymentData.Count(p => p.PaymentReferenceNo != null) + 1;
        int invoiceCounter = _context.PrePaymentData.Count(p => p.InvoiceNo != null) + 1;

        string jobRequestId = $"122{datePart}{jobReqCounter.ToString("D3")}";
        string paymentRefNo = $"902{datePart}{_random.Next(1000, 9999)}";
        string invoiceNo = $"INVO{datePart}{invoiceCounter.ToString("D3")}";

        return (jobRequestId, paymentRefNo, invoiceNo);
    }
}