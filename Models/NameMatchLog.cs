using System.ComponentModel.DataAnnotations;

namespace OfflinePaymentLinks.API.Models;

public class NameMatchLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string JobRequestId { get; set; } = string.Empty;

    public string? NameAsPerProposal { get; set; }
    public string? NameAsPerKyc { get; set; }
    public decimal MatchPercentage { get; set; }
    public string MatchStatus { get; set; } = string.Empty; // "Approved" | "Rejected"
    public string? KycId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}