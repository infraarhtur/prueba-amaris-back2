namespace TechnicalTest.Application.DTOs;

public record TransactionDto(Guid Id, Guid SubscriptionId, int ProductId, decimal Amount, string Type, DateTime OccurredAtUtc);

