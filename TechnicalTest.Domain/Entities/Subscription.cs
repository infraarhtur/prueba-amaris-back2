using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Subscription
{
    private Subscription()
    {
    }

    public Subscription(Guid id, Guid clientId, int productId, decimal amount, DateTime subscribedAtUtc)
    {
        if (amount <= 0)
        {
            throw new DomainException("El monto de vinculación debe ser mayor a cero.");
        }

        Id = id;
        ClientId = clientId;
        ProductId = productId;
        Amount = amount;
        SubscribedAtUtc = subscribedAtUtc;
    }

    public Guid Id { get; private init; }
    public Guid ClientId { get; private init; }
    public int ProductId { get; private init; }
    public decimal Amount { get; private init; }
    public DateTime SubscribedAtUtc { get; private init; }
    public DateTime? CancelledAtUtc { get; private set; }
    public bool IsActive => CancelledAtUtc is null;

    public void Cancel(DateTime cancelledAtUtc)
    {
        if (!IsActive)
        {
            throw new DomainException("La suscripción ya fue cancelada.");
        }

        CancelledAtUtc = cancelledAtUtc;
    }
}

