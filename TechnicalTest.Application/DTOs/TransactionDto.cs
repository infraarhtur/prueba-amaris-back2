namespace TechnicalTest.Application.DTOs;

public record TransactionDto(Guid Id, Guid SubscriptionId, int FundId, decimal Amount, string Type, DateTime OccurredAtUtc);

