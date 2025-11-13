namespace TechnicalTest.Application.DTOs;

public record SubscriptionDto(
    Guid Id,
    Guid ClientId,
    int ProductId,
    decimal Amount,
    DateTime SubscribedAtUtc,
    DateTime? CancelledAtUtc,
    bool IsActive);

