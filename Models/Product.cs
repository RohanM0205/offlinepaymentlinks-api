using System.ComponentModel.DataAnnotations;

namespace OfflinePaymentLinks.API.Models;

public class Product
{
    [Key]
    public int Id { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}