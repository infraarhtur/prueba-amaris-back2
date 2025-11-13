using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Client client, Product product, NotificationChannel channel, CancellationToken cancellationToken);
    Task NotifyCancellationAsync(Client client, Product product, NotificationChannel channel, CancellationToken cancellationToken);
}

