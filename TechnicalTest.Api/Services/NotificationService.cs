using TechnicalTest.Application.Interfaces;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IEventBridgeService _eventBridgeService;

    public NotificationService(
        ILogger<NotificationService> logger,
        IEventBridgeService eventBridgeService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBridgeService = eventBridgeService ?? throw new ArgumentNullException(nameof(eventBridgeService));
    }

    public async Task NotifyAsync(Client client, Product product, NotificationChannel channel, Guid subscriptionId, decimal amount, DateTime subscribedAtUtc, CancellationToken cancellationToken)
    {
        _logger.LogInformation("****cliente {client}: {Message} ******", client.Email, product.Name);
        var message = $"Se ha suscrito al producto {product.Name} por {channel}. Monto disponible: {client.Balance:C}.";
        
        // Validar y loggear el número de teléfono antes de enviar
        if (string.IsNullOrWhiteSpace(client.Phone))
        {
            _logger.LogWarning(
                "⚠️ El cliente {ClientId} ({Email}) no tiene número de teléfono configurado. No se enviará SMS. SubscriptionId: {SubscriptionId}",
                client.Id, client.Email, subscriptionId);
        }
        else
        {
            _logger.LogInformation(
                "📱 Enviando notificación con número de teléfono: {PhoneNumber} para cliente {ClientId} ({Email}). SubscriptionId: {SubscriptionId}",
                client.Phone, client.Id, client.Email, subscriptionId);
            
            // Verificar formato internacional
            if (!client.Phone.StartsWith("+"))
            {
                _logger.LogWarning(
                    "⚠️ El número de teléfono {PhoneNumber} del cliente {ClientId} no tiene formato internacional (debe comenzar con +). Esto puede causar problemas al enviar SMS.",
                    client.Phone, client.Id);
            }
        }
        
        // Publicar evento a EventBridge siempre, independientemente del canal
        // La lambda procesará el evento y enviará tanto SMS como correo
        try
        {
            await _eventBridgeService.PublishSubscriptionCreatedEventAsync(
                subscriptionId: subscriptionId,
                productId: product.Id,
                clientId: client.Id,
                customerEmail: client.Email,
                customerPhone: client.Phone ?? string.Empty,
                amount: amount,
                subscribedAtUtc: subscribedAtUtc,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "✅ Evento SubscriptionCreatedEvent publicado exitosamente a EventBridge. SubscriptionId: {SubscriptionId}, ClientId: {ClientId}, Phone: {Phone}",
                subscriptionId, client.Id, client.Phone ?? "N/A");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al publicar evento SubscriptionCreatedEvent a EventBridge. SubscriptionId: {SubscriptionId}, ClientId: {ClientId}, Phone: {Phone}", 
                subscriptionId, client.Id, client.Phone ?? "N/A");
            // No lanzamos la excepción para no interrumpir el flujo principal
        }

        // Log del canal seleccionado (informacional)
        switch (channel)
        {
            case NotificationChannel.Email:
                _logger.LogInformation("Canal de notificación seleccionado: Email para cliente {ClientId}: {Message}", client.Id, message);
                break;
            case NotificationChannel.Sms:
                _logger.LogInformation("Canal de notificación seleccionado: SMS para cliente {ClientId}: {Message}", client.Id, message);
                break;
        }
    }

    public async Task NotifyCancellationAsync(Client client, Product product, NotificationChannel channel, Guid subscriptionId, decimal amount, DateTime cancelledAtUtc, CancellationToken cancellationToken)
    {
        var clientFullName = $"{client.FirstName} {client.LastName}";
        _logger.LogInformation("****cancelación suscripción - cliente {client}: {product} ******", client.Email, product.Name);
        var message = $"Se ha cancelado su suscripción al producto {product.Name}. Por favor verifique que al cliente {clientFullName} con el email {client.Email} se le haya reintegrado su dinero.";
        
        // Validar y loggear el número de teléfono antes de enviar
        if (string.IsNullOrWhiteSpace(client.Phone))
        {
            _logger.LogWarning(
                "⚠️ El cliente {ClientId} ({Email}) no tiene número de teléfono configurado. No se enviará SMS. SubscriptionId: {SubscriptionId}",
                client.Id, client.Email, subscriptionId);
        }
        else
        {
            _logger.LogInformation(
                "📱 Enviando notificación de cancelación con número de teléfono: {PhoneNumber} para cliente {ClientId} ({Email}). SubscriptionId: {SubscriptionId}",
                client.Phone, client.Id, client.Email, subscriptionId);
            
            // Verificar formato internacional
            if (!client.Phone.StartsWith("+"))
            {
                _logger.LogWarning(
                    "⚠️ El número de teléfono {PhoneNumber} del cliente {ClientId} no tiene formato internacional (debe comenzar con +). Esto puede causar problemas al enviar SMS.",
                    client.Phone, client.Id);
            }
        }
        
        // Publicar evento a EventBridge siempre, independientemente del canal
        // La lambda procesará el evento y enviará tanto SMS como correo
        try
        {
            await _eventBridgeService.PublishSubscriptionCancelledEventAsync(
                subscriptionId: subscriptionId,
                productId: product.Id,
                clientId: client.Id,
                customerEmail: client.Email,
                customerPhone: client.Phone ?? string.Empty,
                amount: amount,
                cancelledAtUtc: cancelledAtUtc,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "✅ Evento SubscriptionCancelledEvent publicado exitosamente a EventBridge. SubscriptionId: {SubscriptionId}, ClientId: {ClientId}, Phone: {Phone}",
                subscriptionId, client.Id, client.Phone ?? "N/A");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al publicar evento SubscriptionCancelledEvent a EventBridge. SubscriptionId: {SubscriptionId}, ClientId: {ClientId}, Phone: {Phone}", 
                subscriptionId, client.Id, client.Phone ?? "N/A");
            // No lanzamos la excepción para no interrumpir el flujo principal
        }

        // Log del canal seleccionado (informacional)
        switch (channel)
        {
            case NotificationChannel.Email:
                _logger.LogInformation("Canal de notificación seleccionado: Email para cliente {ClientId}: {Message}", client.Id, message);
                break;
            case NotificationChannel.Sms:
                _logger.LogInformation("Canal de notificación seleccionado: SMS para cliente {ClientId}: {Message}", client.Id, message);
                break;
        }
    }
}

