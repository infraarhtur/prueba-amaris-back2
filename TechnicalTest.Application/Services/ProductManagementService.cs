using System.Linq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class ProductManagementService : IProductManagementService
{
    private readonly IClientRepository _clientRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public ProductManagementService(
        IClientRepository clientRepository,
        IProductRepository productRepository,
        ISubscriptionRepository subscriptionRepository,
        INotificationService notificationService,
        TimeProvider? timeProvider = null)
    {
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ClientBalanceDto> GetClientAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await GetDefaultClientAsync(cancellationToken).ConfigureAwait(false);
        return client.ToDto();
    }

    public async Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var products = await _productRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return products.Select(product => product.ToDto()).ToArray();
    }

    public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                      ?? throw new DomainException($"No se encontró el producto con id {id}.");
        return product.ToDto();
    }

    public async Task<ProductDto> CreateProductAsync(ProductCreateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await _productRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new DomainException($"Ya existe un producto con id {request.Id}.");
        }

        var category = ParseCategory(request.Category);
        var product = new Product(request.Id, request.Name, request.MinimumAmount, category);

        await _productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        return product.ToDto();
    }

    public async Task<ProductDto> UpdateProductAsync(int id, ProductUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                      ?? throw new DomainException($"No se encontró el producto con id {id}.");

        var category = ParseCategory(request.Category);
        var updated = new Product(existing.Id, request.Name, request.MinimumAmount, category);

        await _productRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated.ToDto();
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                      ?? throw new DomainException($"No se encontró el producto con id {id}.");

        await _productRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscriptions = await _subscriptionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return subscriptions.Select(subscription => subscription.ToDto()).ToArray();
    }

    public async Task<SubscriptionDto> SubscribeAsync(SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
                      ?? throw new DomainException($"No se encontró el producto con id {request.ProductId}.");

        var client = await _clientRepository.GetByIdAsync(request.ClientId, cancellationToken).ConfigureAwait(false)
                     ?? throw new DomainException($"No se encontró el cliente con id {request.ClientId}.");

        var subscriptionAmount = product.MinimumAmount;
        var channel = client.NotificationChannel;

        client.Debit(subscriptionAmount, product.Name);
        await _clientRepository.UpdateAsync(client, cancellationToken).ConfigureAwait(false);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var subscription = new Subscription(Guid.NewGuid(), client.Id, product.Id, subscriptionAmount, nowUtc);
        await _subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        await _notificationService.NotifyAsync(client, product, channel, subscription.Id, subscription.Amount, subscription.SubscribedAtUtc, cancellationToken).ConfigureAwait(false);

        return subscription.ToDto();
    }

    public async Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false)
                          ?? throw new DomainException("No se encontró la suscripción solicitada.");

        if (!subscription.IsActive)
        {
            throw new DomainException("La suscripción ya se encuentra cancelada.");
        }

        var product = await _productRepository.GetByIdAsync(subscription.ProductId, cancellationToken).ConfigureAwait(false)
                      ?? throw new DomainException($"No se encontró el producto con id {subscription.ProductId}.");

        var client = await _clientRepository.GetByIdAsync(subscription.ClientId, cancellationToken).ConfigureAwait(false)
                     ?? throw new DomainException("No se encontró el cliente asociado a la suscripción.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        subscription.Cancel(nowUtc);
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

        client.Credit(subscription.Amount);
        await _clientRepository.UpdateAsync(client, cancellationToken).ConfigureAwait(false);

        var channel = client.NotificationChannel;
        await _notificationService.NotifyCancellationAsync(client, product, channel, subscription.Id, subscription.Amount, subscription.CancelledAtUtc!.Value, cancellationToken).ConfigureAwait(false);

        return subscription.ToDto();
    }

    private async Task<Client> GetDefaultClientAsync(CancellationToken cancellationToken)
    {
        return await _clientRepository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ProductCategory ParseCategory(string category)
    {
        if (Enum.TryParse<ProductCategory>(category, true, out var parsed))
        {
            return parsed;
        }

        throw new DomainException($"Categoría de producto desconocida: {category}.");
    }
}


