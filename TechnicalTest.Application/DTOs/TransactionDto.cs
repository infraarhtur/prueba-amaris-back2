namespace TechnicalTest.Application.DTOs;

public record TransactionDto(Guid TransactionId, Guid SubscriptionId, int FundId, decimal Amount, string Type, DateTime OccurredAtUtc);

