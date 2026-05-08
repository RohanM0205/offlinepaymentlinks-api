namespace OfflinePaymentLinks.API.Models.DTOs;

public record ShortFallSearchRequest(
    string? InwardNumber,
    string? CustomerId,
    string? InteractionId
);

public record SendPaymentRequest(
    PrePaymentData PrePaymentData,
    bool SendEmail,
    bool SendSms
);

public record GeneratePaymentLinkRequest(
    string TransactionType,
    string JobRequestId
);

public record GenerateShortLinkRequest(
    string TransactionType
);