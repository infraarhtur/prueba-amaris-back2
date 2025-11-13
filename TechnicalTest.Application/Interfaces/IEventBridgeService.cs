namespace TechnicalTest.Application.Interfaces;

public interface IEventBridgeService
{
    Task PublishSubscriptionCreatedEventAsync(
        Guid subscriptionId,
        int productId,
        Guid clientId,
        string customerEmail,
        string customerPhone,
        decimal amount,
        DateTime subscribedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishSubscriptionCancelledEventAsync(
        Guid subscriptionId,
        int productId,
        Guid clientId,
        string customerEmail,
        string customerPhone,
        decimal amount,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default);
}

