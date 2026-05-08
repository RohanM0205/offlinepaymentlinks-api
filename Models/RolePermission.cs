using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfflinePaymentLinks.API.Models;

public class RolePermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string RoleId { get; set; } = string.Empty;

    // Module flags
    public bool CanSendPaymentLink { get; set; } = false;
    public bool CanViewReports { get; set; } = false;
    public bool CanManageUsers { get; set; } = false;
    public bool CanApproveUsers { get; set; } = false;
    public bool CanManageRoles { get; set; } = false;
    public bool CanManageAdmins { get; set; } = false;

    // JSON array of allowed transaction types e.g. ["Renewal","Endorsement"]
    // Empty array = all transaction types allowed
    public string AllowedTransactionTypes { get; set; } = "[]";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}