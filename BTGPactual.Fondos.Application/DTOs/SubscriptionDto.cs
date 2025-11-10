namespace BTGPactual.Fondos.Application.DTOs;

public record SubscriptionDto(Guid SubscriptionId, int FundId, decimal Amount, DateTime SubscribedAtUtc, bool IsActive);

