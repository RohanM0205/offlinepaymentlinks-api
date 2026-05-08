using OfflinePaymentLinks.API.Data;
using OfflinePaymentLinks.API.Models;

namespace OfflinePaymentLinks.API.Repositories;

public class GenericPaymentsFetchRepository
{
    private readonly ApplicationDbContext _context;

    public GenericPaymentsFetchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public KYCInformation? GetKycById(string kycId)
        => _context.KYC_Information.FirstOrDefault(k => k.KYC_ID == kycId);

    public PolicyInformation? GetPolicyByNumber(string policyNumber)
        => _context.PolicyInformation.FirstOrDefault(p => p.PolicyNumber == policyNumber);

    public PolicyInformation? GetPolicyByInwardNumber(string inwardNumber)
        => _context.PolicyInformation.FirstOrDefault(p => p.InwardNumber == inwardNumber);

    public PolicyInformation? GetPolicyByCustomerId(string customerId)
        => _context.PolicyInformation.FirstOrDefault(p => p.CustomerId == customerId);

    public PolicyInformation? GetPolicyByInteractionId(string interactionId)
        => _context.PolicyInformation.FirstOrDefault(p => p.InteractionId == interactionId);

    public PinCodeData? GetPinCodeDetails(string pinCode)
        => _context.PinCodeData.FirstOrDefault(p => p.PinCode == pinCode);
}