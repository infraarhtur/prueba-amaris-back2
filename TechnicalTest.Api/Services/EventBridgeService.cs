using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechnicalTest.Application.Interfaces;

namespace TechnicalTest.Api.Services;

public class EventBridgeService : IEventBridgeService
{
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly ILogger<EventBridgeService> _logger;
    private readonly string _eventBusName;
    private const string EventSource = "technicaltest.subscriptions";

    public EventBridgeService(
        IAmazonEventBridge eventBridgeClient,
        IConfiguration configuration,
        ILogger<EventBridgeService> logger)
    {
        _eventBridgeClient = eventBridgeClient ?? throw new ArgumentNullException(nameof(eventBridgeClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var eventBusName = configuration["AWS:EventBridge:EventBusName"];
        _eventBusName = string.IsNullOrWhiteSpace(eventBusName)
            ? throw new InvalidOperationException("AWS:EventBridge:EventBusName configuration is required.")
            : eventBusName;
    }

    public async Task PublishSubscriptionCreatedEventAsync(
        Guid subscriptionId,
        int productId,
        Guid clientId,
        string customerEmail,
        string customerPhone,
        decimal amount,
        DateTime subscribedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var detail = new
        {
            subscriptionId = subscriptionId.ToString(),
            productId,
            clientId = clientId.ToString(),
            customerEmail,
            customerPhone,
            amount,
            subscribedAtUtc = subscribedAtUtc.ToString("O")
        };

        var detailJson = JsonSerializer.Serialize(detail);
        
        // Log del detalle completo del evento para verificación
        _logger.LogDebug(
            "Detalle completo del evento SubscriptionCreatedEvent: {EventDetail}",
            detailJson);

        var request = new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new PutEventsRequestEntry
                {
                    Source = EventSource,
                    DetailType = "SubscriptionCreatedEvent",
                    Detail = detailJson,
                    EventBusName = _eventBusName
                }
            }
        };

        try
        {
            _logger.LogInformation(
                "📤 Publicando SubscriptionCreatedEvent a EventBridge. SubscriptionId: {SubscriptionId}, ProductId: {ProductId}, ClientId: {ClientId}, Email: {Email}, Phone: {Phone}",
                subscriptionId, productId, clientId, customerEmail, customerPhone ?? "N/A");

            var response = await _eventBridgeClient.PutEventsAsync(request, cancellationToken);

            if (response.FailedEntryCount > 0)
            {
                var errors = string.Join(", ", response.Entries
                    .Where(e => e.ErrorCode != null || e.ErrorMessage != null)
                    .Select(e => $"{e.ErrorCode}: {e.ErrorMessage}"));

                _logger.LogError(
                    "Failed to publish SubscriptionCreatedEvent to EventBridge. Errors: {Errors}",
                    errors);
            }
            else
            {
                _logger.LogInformation(
                    "✅ SubscriptionCreatedEvent publicado exitosamente a EventBridge. EventId: {EventId}, SubscriptionId: {SubscriptionId}, Phone: {Phone}",
                    response.Entries.FirstOrDefault()?.EventId, subscriptionId, customerPhone ?? "N/A");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception while publishing SubscriptionCreatedEvent to EventBridge. SubscriptionId: {SubscriptionId}",
                subscriptionId);
            throw;
        }
    }

    public async Task PublishSubscriptionCancelledEventAsync(
        Guid subscriptionId,
        int productId,
        Guid clientId,
        string customerEmail,
        string customerPhone,
        decimal amount,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var detail = new
        {
            subscriptionId = subscriptionId.ToString(),
            productId,
            clientId = clientId.ToString(),
            customerEmail,
            customerPhone,
            amount,
            cancelledAtUtc = cancelledAtUtc.ToString("O")
        };

        var detailJson = JsonSerializer.Serialize(detail);
        
        // Log del detalle completo del evento para verificación
        _logger.LogDebug(
            "Detalle completo del evento SubscriptionCancelledEvent: {EventDetail}",
            detailJson);

        var request = new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new PutEventsRequestEntry
                {
                    Source = EventSource,
                    DetailType = "SubscriptionCancelledEvent",
                    Detail = detailJson,
                    EventBusName = _eventBusName
                }
            }
        };

        try
        {
            _logger.LogInformation(
                "📤 Publicando SubscriptionCancelledEvent a EventBridge. SubscriptionId: {SubscriptionId}, ProductId: {ProductId}, ClientId: {ClientId}, Email: {Email}, Phone: {Phone}",
                subscriptionId, productId, clientId, customerEmail, customerPhone ?? "N/A");

            var response = await _eventBridgeClient.PutEventsAsync(request, cancellationToken);

            if (response.FailedEntryCount > 0)
            {
                var errors = string.Join(", ", response.Entries
                    .Where(e => e.ErrorCode != null || e.ErrorMessage != null)
                    .Select(e => $"{e.ErrorCode}: {e.ErrorMessage}"));

                _logger.LogError(
                    "Failed to publish SubscriptionCancelledEvent to EventBridge. Errors: {Errors}",
                    errors);
            }
            else
            {
                _logger.LogInformation(
                    "✅ SubscriptionCancelledEvent publicado exitosamente a EventBridge. EventId: {EventId}, SubscriptionId: {SubscriptionId}, Phone: {Phone}",
                    response.Entries.FirstOrDefault()?.EventId, subscriptionId, customerPhone ?? "N/A");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception while publishing SubscriptionCancelledEvent to EventBridge. SubscriptionId: {SubscriptionId}",
                subscriptionId);
            throw;
        }
    }
}

