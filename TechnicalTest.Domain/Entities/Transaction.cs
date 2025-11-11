using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Domain.Entities;

public class Transaction
{
    private Transaction()
    {
    }

    public Transaction(Guid id, Guid subscriptionId, int fundId, decimal amount, TransactionType type, DateTime occurredAtUtc)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        FundId = fundId;
        Amount = amount;
        Type = type;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public int FundId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
}

