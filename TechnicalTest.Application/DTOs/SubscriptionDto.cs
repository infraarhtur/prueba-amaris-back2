namespace TechnicalTest.Application.DTOs;

public record SubscriptionDto(
    Guid Id,
    Guid ClientId,
    int FundId,
    decimal Amount,
    DateTime SubscribedAtUtc,
    DateTime? CancelledAtUtc,
    bool IsActive);

