using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Client client, Product product, NotificationChannel channel, Guid subscriptionId, decimal amount, DateTime subscribedAtUtc, CancellationToken cancellationToken);
    Task NotifyCancellationAsync(Client client, Product product, NotificationChannel channel, Guid subscriptionId, decimal amount, DateTime cancelledAtUtc, CancellationToken cancellationToken);
}

