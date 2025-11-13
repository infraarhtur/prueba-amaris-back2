using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.RegularExpressions;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TechnicalTest.Api.Services;

public class SnsSubscriptionService : ISnsSubscriptionService
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<SnsSubscriptionService> _logger;
    private readonly string _topicArn;

    public SnsSubscriptionService(
        IAmazonSimpleNotificationService snsClient,
        ILogger<SnsSubscriptionService> logger,
        IConfiguration configuration)
    {
        _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _topicArn = configuration["AWS:SNS:TopicArn"] 
            ?? throw new InvalidOperationException("AWS:SNS:TopicArn no está configurado en appsettings.json");
    }

    public async Task<string> SubscribePhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("El número de teléfono no puede estar vacío", nameof(phoneNumber));
        }

        // Validar formato del número
        if (!Regex.IsMatch(phoneNumber, @"^\+[1-9]\d{1,14}$"))
        {
            throw new ArgumentException("El número de teléfono debe tener el formato internacional: +[código país][número]", nameof(phoneNumber));
        }

        try
        {
            _logger.LogInformation("Suscribiendo número de teléfono {PhoneNumber} al topic SNS {TopicArn}", phoneNumber, _topicArn);

            var request = new SubscribeRequest
            {
                TopicArn = _topicArn,
                Protocol = "sms",
                Endpoint = phoneNumber
            };

            var response = await _snsClient.SubscribeAsync(request, cancellationToken);

            _logger.LogInformation(
                "Número de teléfono {PhoneNumber} suscrito exitosamente. SubscriptionArn: {SubscriptionArn}",
                phoneNumber,
                response.SubscriptionArn);

            return response.SubscriptionArn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al suscribir número de teléfono {PhoneNumber} al topic SNS", phoneNumber);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string subscriptionArn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionArn))
        {
            throw new ArgumentException("El ARN de suscripción no puede estar vacío", nameof(subscriptionArn));
        }

        try
        {
            _logger.LogInformation("Cancelando suscripción SNS: {SubscriptionArn}", subscriptionArn);

            var request = new UnsubscribeRequest
            {
                SubscriptionArn = subscriptionArn
            };

            await _snsClient.UnsubscribeAsync(request, cancellationToken);

            _logger.LogInformation("Suscripción SNS cancelada exitosamente: {SubscriptionArn}", subscriptionArn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar suscripción SNS {SubscriptionArn}", subscriptionArn);
            throw;
        }
    }

    public async Task<IEnumerable<SnsSubscriptionDto>> ListSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listando suscripciones del topic SNS: {TopicArn}", _topicArn);

            var request = new ListSubscriptionsByTopicRequest
            {
                TopicArn = _topicArn
            };

            var response = await _snsClient.ListSubscriptionsByTopicAsync(request, cancellationToken);

            var subscriptions = response.Subscriptions.Select(sub => new SnsSubscriptionDto
            {
                SubscriptionArn = sub.SubscriptionArn,
                Protocol = sub.Protocol,
                Endpoint = sub.Endpoint,
                TopicArn = sub.TopicArn
            }).ToList();

            _logger.LogInformation("Se encontraron {Count} suscripciones en el topic SNS", subscriptions.Count);

            return subscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar suscripciones del topic SNS {TopicArn}", _topicArn);
            throw;
        }
    }
}

