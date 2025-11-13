using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TechnicalTest.Api.Services;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using Xunit;
using DomainClient = TechnicalTest.Domain.Entities.Client;
using DomainProduct = TechnicalTest.Domain.Entities.Product;

namespace TechnicalTest.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _logger = new();
    private readonly Mock<IEventBridgeService> _eventBridgeService = new();

    private NotificationService CreateSut() =>
        new(_logger.Object, _eventBridgeService.Object);

    private static DomainClient CreateClient(
        Guid? id = null,
        string? phone = "+573208965783",
        NotificationChannel channel = NotificationChannel.Email) =>
        new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "John",
            "Doe",
            "Bogotá",
            "john.doe@example.com",
            phone ?? "+573208965783",
            1000m,
            channel);

    private static DomainProduct CreateProduct(int id = 1, string name = "Fondo Test") =>
        new(id, name, 100m, ProductCategory.FPV);

    [Fact]
    public async Task NotifyAsync_WithValidPhone_ShouldLogPhoneNumberAndCallEventBridge()
    {
        // Arrange
        var sut = CreateSut();
        var client = CreateClient(phone: "+573208965783");
        var product = CreateProduct();
        var subscriptionId = Guid.NewGuid();
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        await sut.NotifyAsync(client, product, NotificationChannel.Sms, subscriptionId, amount, subscribedAtUtc, cancellationToken);

        // Assert
        _eventBridgeService.Verify(
            service => service.PublishSubscriptionCreatedEventAsync(
                subscriptionId,
                product.Id,
                client.Id,
                client.Email,
                "+573208965783",
                amount,
                subscribedAtUtc,
                cancellationToken),
            Times.Once);

        // Verificar que se logueó el número de teléfono
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("+573208965783")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotifyAsync_WithPhoneWithoutInternationalFormat_ShouldLogWarningAndCallEventBridge()
    {
        // Arrange
        var sut = CreateSut();
        // Usar un teléfono sin formato internacional (sin +)
        var client = CreateClient(phone: "573208965783");
        var product = CreateProduct();
        var subscriptionId = Guid.NewGuid();
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        await sut.NotifyAsync(client, product, NotificationChannel.Sms, subscriptionId, amount, subscribedAtUtc, cancellationToken);

        // Assert
        _eventBridgeService.Verify(
            service => service.PublishSubscriptionCreatedEventAsync(
                subscriptionId,
                product.Id,
                client.Id,
                client.Email,
                "573208965783",
                amount,
                subscribedAtUtc,
                cancellationToken),
            Times.Once);

        // Verificar que se logueó la advertencia de formato
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("formato internacional")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotifyAsync_WithPhoneWithoutInternationalFormat_ShouldLogWarning()
    {
        // Arrange
        var sut = CreateSut();
        var client = CreateClient(phone: "573208965783"); // Sin el +
        var product = CreateProduct();
        var subscriptionId = Guid.NewGuid();
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        await sut.NotifyAsync(client, product, NotificationChannel.Sms, subscriptionId, amount, subscribedAtUtc, cancellationToken);

        // Assert
        _eventBridgeService.Verify(
            service => service.PublishSubscriptionCreatedEventAsync(
                subscriptionId,
                product.Id,
                client.Id,
                client.Email,
                "573208965783",
                amount,
                subscribedAtUtc,
                cancellationToken),
            Times.Once);

        // Verificar que se logueó la advertencia de formato
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("formato internacional")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotifyCancellationAsync_WithValidPhone_ShouldLogPhoneNumberAndCallEventBridge()
    {
        // Arrange
        var sut = CreateSut();
        var client = CreateClient(phone: "+573208965783");
        var product = CreateProduct();
        var subscriptionId = Guid.NewGuid();
        var amount = 100m;
        var cancelledAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        await sut.NotifyCancellationAsync(client, product, NotificationChannel.Sms, subscriptionId, amount, cancelledAtUtc, cancellationToken);

        // Assert
        _eventBridgeService.Verify(
            service => service.PublishSubscriptionCancelledEventAsync(
                subscriptionId,
                product.Id,
                client.Id,
                client.Email,
                "+573208965783",
                amount,
                cancelledAtUtc,
                cancellationToken),
            Times.Once);

        // Verificar que se logueó el número de teléfono
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("+573208965783")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotifyCancellationAsync_WithPhoneWithoutInternationalFormat_ShouldLogWarningAndCallEventBridge()
    {
        // Arrange
        var sut = CreateSut();
        // Usar un teléfono sin formato internacional (sin +)
        var client = CreateClient(phone: "573208965783");
        var product = CreateProduct();
        var subscriptionId = Guid.NewGuid();
        var amount = 100m;
        var cancelledAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        await sut.NotifyCancellationAsync(client, product, NotificationChannel.Sms, subscriptionId, amount, cancelledAtUtc, cancellationToken);

        // Assert
        _eventBridgeService.Verify(
            service => service.PublishSubscriptionCancelledEventAsync(
                subscriptionId,
                product.Id,
                client.Id,
                client.Email,
                "573208965783",
                amount,
                cancelledAtUtc,
                cancellationToken),
            Times.Once);

        // Verificar que se logueó la advertencia de formato
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("formato internacional")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task NotifyAsync_WhenEventBridgeThrowsException_ShouldLogErrorButNotThrow()
    {
        // Arrange
        var sut = CreateSut();
        var client = CreateClient(phone: "+573208965783");
        var product = CreateProduct();
        var subscriptionId = Guid.NewGuid();
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        _eventBridgeService
            .Setup(service => service.PublishSubscriptionCreatedEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("EventBridge error"));

        // Act
        var act = async () => await sut.NotifyAsync(client, product, NotificationChannel.Sms, subscriptionId, amount, subscribedAtUtc, cancellationToken);

        // Assert - No debe lanzar excepción
        await act.Should().NotThrowAsync();

        // Verificar que se logueó el error
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error al publicar evento")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

