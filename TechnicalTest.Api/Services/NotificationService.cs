using TechnicalTest.Application.Interfaces;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Api.Services;

public class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    public Task NotifyAsync(Client client, Fund fund, NotificationChannel channel, CancellationToken cancellationToken)
    {
        var message = $"Se ha suscrito al fondo {fund.Name} por {channel}. Monto disponible: {client.Balance:C}.";
        switch (channel)
        {
            case NotificationChannel.Email:
                logger.LogInformation("Enviando correo a cliente {ClientId}: {Message}", client.Id, message);
                break;
            case NotificationChannel.Sms:
                logger.LogInformation("Enviando SMS a cliente {ClientId}: {Message}", client.Id, message);
                break;
        }

        return Task.CompletedTask;
    }
}

