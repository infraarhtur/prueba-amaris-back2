using TechnicalTest.Application.Interfaces;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Api.Services;

public class NotificationService(ILogger<NotificationService> logger) : INotificationService
{
    public Task NotifyAsync(Client client, Product product, NotificationChannel channel, CancellationToken cancellationToken)
    {
         logger.LogInformation("****cliente {client}: {Message} ******", client.Email,product.Name);
        var message = $"Se ha suscrito al producto {product.Name} por {channel}. Monto disponible: {client.Balance:C}.";
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

    public Task NotifyCancellationAsync(Client client, Product product, NotificationChannel channel, CancellationToken cancellationToken)
    {
        var clientFullName = $"{client.FirstName} {client.LastName}";
        logger.LogInformation("****cancelación suscripción - cliente {client}: {product} ******", client.Email, product.Name);
        var message = $"Se ha cancelado su suscripción al producto {product.Name}. Por favor verifique que al cliente {clientFullName} con el email {client.Email} se le haya reintegrado su dinero.";
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

