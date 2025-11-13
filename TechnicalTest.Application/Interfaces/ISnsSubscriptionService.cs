using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface ISnsSubscriptionService
{
    Task<string> SubscribePhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string subscriptionArn, CancellationToken cancellationToken = default);
    Task<IEnumerable<SnsSubscriptionDto>> ListSubscriptionsAsync(CancellationToken cancellationToken = default);
}

