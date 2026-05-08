using System.ComponentModel.DataAnnotations;

namespace OfflinePaymentLinks.API.Models;

public class PostPaymentData
{
    [Key]
    public int Id { get; set; }

    // Link to PrePaymentData
    public string? JobRequestId { get; set; }
    public string? InvoiceNo { get; set; }
    public string? PaymentReferenceNo { get; set; }

    // Customer
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerMobile { get; set; }
    public string? PolicyNumber { get; set; }
    public string? TransactionType { get; set; }
    public string? Product { get; set; }
    public string? ProductType { get; set; }
    public string? ProductCode { get; set; }
    public decimal? Amount { get; set; }
    public string? ChannelType { get; set; }
    public string? CustomerType { get; set; }

    // Payment details
    public string? PaymentMode { get; set; } // UPI | CC | DC | NB | WALLET
    public string? PaymentTransactionNumber { get; set; } // generated on our end
    public string? PaymentId { get; set; } // gateway payment ID (success only)
    public string? PaymentStatus { get; set; } // "Success" | "Failed"
    public string? FailureReason { get; set; }
    public string? FailureCode { get; set; }
    public DateTime? PaymentAttemptedAt { get; set; }
    public DateTime? PaymentSuccessAt { get; set; }

    // Gateway / instrument details
    public string? UpiId { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardNetwork { get; set; } // Visa, Mastercard, Rupay
    public string? CardType { get; set; } // Credit | Debit
    public string? BankName { get; set; }
    public string? WalletName { get; set; }

    // Meta
    public string? RequestorEmail { get; set; }
    public string? LinkSharedBy { get; set; }
    public string? Remarks { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastUpdated { get; set; }
}