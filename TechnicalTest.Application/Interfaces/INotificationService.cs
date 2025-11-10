using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Client client, Fund fund, NotificationChannel channel, CancellationToken cancellationToken);
}

