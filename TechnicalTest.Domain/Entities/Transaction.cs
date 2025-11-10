using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Domain.Entities;

public class Transaction
{
    public Transaction(Guid id, Guid subscriptionId, int fundId, decimal amount, TransactionType type, DateTime occurredAtUtc)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        FundId = fundId;
        Amount = amount;
        Type = type;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }
    public Guid SubscriptionId { get; }
    public int FundId { get; }
    public decimal Amount { get; }
    public TransactionType Type { get; }
    public DateTime OccurredAtUtc { get; }
}

