using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Moq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Services;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;
using Xunit;
using DomainClient = TechnicalTest.Domain.Entities.Client;
using DomainProduct = TechnicalTest.Domain.Entities.Product;
using DomainSubscription = TechnicalTest.Domain.Entities.Subscription;

namespace TechnicalTest.Tests.Subscriptions;

public partial class ProductManagementSubscriptionServiceTests
{
    private static readonly Guid DefaultClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DefaultSubscriptionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid DefaultUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2025-02-03T10:15:20Z");

    private readonly Mock<IClientRepository> _clientRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);

    private ProductManagementService CreateSut() =>
        new(
            _clientRepository.Object,
            _productRepository.Object,
            _subscriptionRepository.Object,
            _notificationService.Object,
            _timeProvider);

    private static DomainProduct CreateProduct(
        int id = 10,
        string name = "Fondo Conservador",
        decimal minimumAmount = 150m,
        ProductCategory category = ProductCategory.FPV) =>
        new(id, name, minimumAmount, category);

    private static DomainClient CreateClient(
        Guid? id = null,
        decimal balance = 1_000m,
        NotificationChannel channel = NotificationChannel.Email) =>
        new(
            id ?? DefaultClientId,
            DefaultUserId,
            "Alice",
            "Smith",
            "Madrid",
            "alice.smith@example.com",
            "+1234567890",
            balance,
            channel);

    private static DomainSubscription CreateSubscription(
        Guid? id = null,
        Guid? clientId = null,
        int productId = 50,
        decimal amount = 200m,
        DateTime? subscribedAtUtc = null) =>
        new(
            id ?? DefaultSubscriptionId,
            clientId ?? DefaultClientId,
            productId,
            amount,
            subscribedAtUtc ?? FixedNow.UtcDateTime);

    private static SubscriptionDto CreateSubscriptionDto(DomainSubscription subscription) =>
        new(
            subscription.Id,
            subscription.ClientId,
            subscription.ProductId,
            subscription.Amount,
            subscription.SubscribedAtUtc,
            subscription.CancelledAtUtc,
            subscription.IsActive);

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldReturnMappedSubscriptions()
    {
        var sut = CreateSut();
        IReadOnlyCollection<DomainSubscription> subscriptions =
        [
            CreateSubscription(Guid.Parse("11111111-1111-1111-1111-111111111111"), productId: 10, amount: 120m),
            CreateSubscription(Guid.Parse("22222222-2222-2222-2222-222222222222"), productId: 20, amount: 300m)
        ];
        var cancellationToken = CancellationToken.None;
        _subscriptionRepository
            .Setup(repository => repository.GetAllAsync(cancellationToken))
            .ReturnsAsync(subscriptions);

        var result = await sut.GetSubscriptionsAsync(cancellationToken);

        result.Should().BeEquivalentTo(
            subscriptions.Select(subscription => subscription.ToDto()).ToImmutableArray());
        _subscriptionRepository.Verify(repository => repository.GetAllAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_WhenCancellationRequested_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = async () => await sut.GetSubscriptionsAsync(cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _subscriptionRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SubscribeAsync_ShouldCreateSubscriptionDebitClientAndNotify()
    {
        var sut = CreateSut();
        var product = CreateProduct(id: 40, minimumAmount: 250m, name: "Fondo Crecimiento");
        var client = CreateClient(balance: 600m);
        var request = new SubscriptionRequestDto(product.Id, client.Id);
        using var cts = new CancellationTokenSource();
        DomainSubscription? capturedSubscription = null;

        _productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(client.Id, cts.Token))
            .ReturnsAsync(client);
        _clientRepository
            .Setup(repository => repository.UpdateAsync(client, cts.Token))
            .Returns(Task.CompletedTask);
        _subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), cts.Token))
            .Callback<DomainSubscription, CancellationToken>((subscription, _) => capturedSubscription = subscription)
            .Returns(Task.CompletedTask);
        _notificationService
            .Setup(service => service.NotifyAsync(client, product, client.NotificationChannel, It.IsAny<Guid>(), product.MinimumAmount, FixedNow.UtcDateTime, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.SubscribeAsync(request, cts.Token);

        client.Balance.Should().Be(350m);
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.ClientId.Should().Be(client.Id);
        capturedSubscription.ProductId.Should().Be(product.Id);
        capturedSubscription.Amount.Should().Be(product.MinimumAmount);
        capturedSubscription.SubscribedAtUtc.Should().Be(FixedNow.UtcDateTime);
        result.Should().BeEquivalentTo(capturedSubscription.ToDto());
        _clientRepository.Verify(repository => repository.UpdateAsync(client, cts.Token), Times.Once);
        _subscriptionRepository.Verify(repository => repository.AddAsync(capturedSubscription, cts.Token), Times.Once);
        _notificationService.Verify(service => service.NotifyAsync(client, product, client.NotificationChannel, capturedSubscription!.Id, product.MinimumAmount, FixedNow.UtcDateTime, cts.Token), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WhenProductDoesNotExist_ShouldThrowDomainException()
    {
        var sut = CreateSut();
        var request = new SubscriptionRequestDto(70, Guid.NewGuid());
        using var cts = new CancellationTokenSource();

        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.SubscribeAsync(request, cts.Token);

        await act.Should()
            .ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró el producto con id {request.ProductId}.");
        _subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(service => service.NotifyAsync(It.IsAny<DomainClient>(), It.IsAny<DomainProduct>(), It.IsAny<NotificationChannel>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WhenClientDoesNotExist_ShouldThrowDomainException()
    {
        var sut = CreateSut();
        var product = CreateProduct(id: 80);
        var request = new SubscriptionRequestDto(product.Id, Guid.NewGuid());
        using var cts = new CancellationTokenSource();

        _productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(request.ClientId, cts.Token))
            .ReturnsAsync((DomainClient?)null);

        var act = async () => await sut.SubscribeAsync(request, cts.Token);

        await act.Should()
            .ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró el cliente con id {request.ClientId}.");
        _subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(service => service.NotifyAsync(It.IsAny<DomainClient>(), It.IsAny<DomainProduct>(), It.IsAny<NotificationChannel>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.SubscribeAsync(null!, CancellationToken.None);

        await act.Should()
            .ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SubscribeAsync_WhenCancellationRequested_ShouldThrow()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.SubscribeAsync(new SubscriptionRequestDto(90, Guid.NewGuid()), cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _subscriptionRepository.VerifyNoOtherCalls();
        _notificationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldCancelAndRefundClient()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription(amount: 180m, productId: 55);
        var product = CreateProduct(subscription.ProductId, "Bono Regional", subscription.Amount, ProductCategory.FIC);
        var client = CreateClient(id: subscription.ClientId, balance: 320m);
        using var cts = new CancellationTokenSource();

        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);
        _subscriptionRepository
            .Setup(repository => repository.UpdateAsync(subscription, cts.Token))
            .Returns(Task.CompletedTask);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ProductId, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ClientId, cts.Token))
            .ReturnsAsync(client);
        _clientRepository
            .Setup(repository => repository.UpdateAsync(client, cts.Token))
            .Returns(Task.CompletedTask);
        _notificationService
            .Setup(service => service.NotifyCancellationAsync(client, product, client.NotificationChannel, subscription.Id, subscription.Amount, FixedNow.UtcDateTime, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        subscription.IsActive.Should().BeFalse();
        subscription.CancelledAtUtc.Should().Be(FixedNow.UtcDateTime);
        client.Balance.Should().Be(500m);
        result.Should().BeEquivalentTo(CreateSubscriptionDto(subscription));
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(subscription, cts.Token), Times.Once);
        _clientRepository.Verify(repository => repository.UpdateAsync(client, cts.Token), Times.Once);
        _notificationService.Verify(service => service.NotifyCancellationAsync(client, product, client.NotificationChannel, subscription.Id, subscription.Amount, FixedNow.UtcDateTime, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenSubscriptionDoesNotExist_ShouldThrowDomainException()
    {
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscriptionId, cts.Token))
            .ReturnsAsync((DomainSubscription?)null);

        var act = async () => await sut.CancelSubscriptionAsync(subscriptionId, cts.Token);

        await act.Should()
            .ThrowExactlyAsync<DomainException>()
            .WithMessage("No se encontró la suscripción solicitada.");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenSubscriptionAlreadyCancelled_ShouldThrowDomainException()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription();
        subscription.Cancel(DateTime.UtcNow);
        using var cts = new CancellationTokenSource();

        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);

        var act = async () => await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        await act.Should()
            .ThrowExactlyAsync<DomainException>()
            .WithMessage("La suscripción ya se encuentra cancelada.");
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenProductDoesNotExist_ShouldThrowDomainException()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription(productId: 75);
        using var cts = new CancellationTokenSource();

        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ProductId, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        await act.Should()
            .ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró el producto con id {subscription.ProductId}.");
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenClientDoesNotExist_ShouldThrowDomainException()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription();
        var product = CreateProduct(subscription.ProductId);
        using var cts = new CancellationTokenSource();

        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ProductId, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ClientId, cts.Token))
            .ReturnsAsync((DomainClient?)null);

        var act = async () => await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        await act.Should()
            .ThrowExactlyAsync<DomainException>()
            .WithMessage("No se encontró el cliente asociado a la suscripción.");
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainClient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WhenCancellationRequested_ShouldThrow()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.CancelSubscriptionAsync(Guid.NewGuid(), cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _subscriptionRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

