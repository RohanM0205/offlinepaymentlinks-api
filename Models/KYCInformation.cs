namespace OfflinePaymentLinks.API.Models;

public class KYCInformation
{
    public string KYC_ID { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? PAN_Number { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Pin_Code { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? KYC_Status { get; set; }
    public DateTime? DOB { get; set; }
    public string? CustomerType { get; set; }
}