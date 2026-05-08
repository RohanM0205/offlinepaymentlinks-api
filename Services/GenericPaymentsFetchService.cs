using OfflinePaymentLinks.API.Models;
using OfflinePaymentLinks.API.Repositories;

namespace OfflinePaymentLinks.API.Services;

public class GenericPaymentsFetchService
{
    private readonly GenericPaymentsFetchRepository _repository;

    public GenericPaymentsFetchService(GenericPaymentsFetchRepository repository)
    {
        _repository = repository;
    }

    public KYCInformation? FetchKYC(string kycId)
        => _repository.GetKycById(kycId);

    public PolicyInformation? GetPolicyDetails(string policyNumber)
        => _repository.GetPolicyByNumber(policyNumber);

    public PolicyInformation? ShortFallSearch(
        string? inwardNumber = null,
        string? customerId = null,
        string? interactionId = null)
    {
        if (!string.IsNullOrWhiteSpace(inwardNumber))
            return _repository.GetPolicyByInwardNumber(inwardNumber);
        if (!string.IsNullOrWhiteSpace(customerId))
            return _repository.GetPolicyByCustomerId(customerId);
        if (!string.IsNullOrWhiteSpace(interactionId))
            return _repository.GetPolicyByInteractionId(interactionId);
        return null;
    }

    public PinCodeData? GetPinCodeInformation(string pinCode)
        => _repository.GetPinCodeDetails(pinCode);
}