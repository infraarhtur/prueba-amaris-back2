using BTGPactual.Fondos.Domain.Entities;
using BTGPactual.Fondos.Domain.Enums;

namespace BTGPactual.Fondos.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Client client, Fund fund, NotificationChannel channel, CancellationToken cancellationToken);
}

