namespace OfflinePaymentLinks.API.Models;

public class UrlMapping
{
    public int Id { get; set; }
    public string ShortCode { get; set; }
    public string OriginalUrl { get; set; }
    public string ShortUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
}