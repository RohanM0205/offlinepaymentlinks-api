using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OfflinePaymentLinks.API.Models;

public class ApplicationUser : IdentityUser
{
    public bool IsApproved { get; set; } = false;
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
    [Column("ApprovedDate")]
    public DateTime? Approveddate { get; set; }
    [Column("ApproverName")]
    public string? ApproverName { get; set; }

}