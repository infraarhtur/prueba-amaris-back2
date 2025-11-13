using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechnicalTest.Api.Services;
using Xunit;

namespace TechnicalTest.Tests.Services;

public class EventBridgeServiceTests
{
    private readonly Mock<IAmazonEventBridge> _eventBridgeClient = new();
    private readonly Mock<ILogger<EventBridgeService>> _logger = new();
    private readonly IConfiguration _configuration;

    public EventBridgeServiceTests()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "AWS:EventBridge:EventBusName", "test-bus" }
        });
        _configuration = configurationBuilder.Build();
    }

    private EventBridgeService CreateSut() =>
        new(_eventBridgeClient.Object, _configuration, _logger.Object);

    [Fact]
    public async Task PublishSubscriptionCreatedEventAsync_ShouldIncludePhoneNumberInEvent()
    {
        // Arrange
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var productId = 1;
        var clientId = Guid.NewGuid();
        var customerEmail = "test@example.com";
        var customerPhone = "+573208965783";
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        PutEventsRequest? capturedRequest = null;
        _eventBridgeClient
            .Setup(client => client.PutEventsAsync(It.IsAny<PutEventsRequest>(), cancellationToken))
            .Callback<PutEventsRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutEventsResponse
            {
                Entries = new List<PutEventsResultEntry>
                {
                    new PutEventsResultEntry { EventId = "test-event-id" }
                },
                FailedEntryCount = 0
            });

        // Act
        await sut.PublishSubscriptionCreatedEventAsync(
            subscriptionId,
            productId,
            clientId,
            customerEmail,
            customerPhone,
            amount,
            subscribedAtUtc,
            cancellationToken);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Entries.Should().HaveCount(1);
        var entry = capturedRequest.Entries[0];
        // El JSON serializa el + como \u002B, así que verificamos ambos formatos
        entry.Detail.Should().Match(d => d.Contains(customerPhone) || d.Contains("\\u002B573208965783"));
        entry.Detail.Should().Contain(customerEmail);
        entry.Detail.Should().Contain(subscriptionId.ToString());
        entry.Detail.Should().Contain(productId.ToString());
        entry.Detail.Should().Contain(clientId.ToString());

        // Verificar que se logueó el número de teléfono (puede estar en formato normal o unicode)
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains(customerPhone) || (v.ToString() ?? string.Empty).Contains("573208965783")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishSubscriptionCreatedEventAsync_WithEmptyPhone_ShouldIncludeEmptyPhoneInEvent()
    {
        // Arrange
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var productId = 1;
        var clientId = Guid.NewGuid();
        var customerEmail = "test@example.com";
        var customerPhone = string.Empty;
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        PutEventsRequest? capturedRequest = null;
        _eventBridgeClient
            .Setup(client => client.PutEventsAsync(It.IsAny<PutEventsRequest>(), cancellationToken))
            .Callback<PutEventsRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutEventsResponse
            {
                Entries = new List<PutEventsResultEntry>
                {
                    new PutEventsResultEntry { EventId = "test-event-id" }
                },
                FailedEntryCount = 0
            });

        // Act
        await sut.PublishSubscriptionCreatedEventAsync(
            subscriptionId,
            productId,
            clientId,
            customerEmail,
            customerPhone,
            amount,
            subscribedAtUtc,
            cancellationToken);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Entries.Should().HaveCount(1);
        var entry = capturedRequest.Entries[0];
        entry.Detail.Should().Contain("\"customerPhone\":\"\"");
        entry.Detail.Should().Contain(customerEmail);

        // Verificar que se logueó el teléfono vacío
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Phone:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishSubscriptionCancelledEventAsync_ShouldIncludePhoneNumberInEvent()
    {
        // Arrange
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var productId = 1;
        var clientId = Guid.NewGuid();
        var customerEmail = "test@example.com";
        var customerPhone = "+573208965783";
        var amount = 100m;
        var cancelledAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        PutEventsRequest? capturedRequest = null;
        _eventBridgeClient
            .Setup(client => client.PutEventsAsync(It.IsAny<PutEventsRequest>(), cancellationToken))
            .Callback<PutEventsRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new PutEventsResponse
            {
                Entries = new List<PutEventsResultEntry>
                {
                    new PutEventsResultEntry { EventId = "test-event-id" }
                },
                FailedEntryCount = 0
            });

        // Act
        await sut.PublishSubscriptionCancelledEventAsync(
            subscriptionId,
            productId,
            clientId,
            customerEmail,
            customerPhone,
            amount,
            cancelledAtUtc,
            cancellationToken);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Entries.Should().HaveCount(1);
        var entry = capturedRequest.Entries[0];
        // El JSON serializa el + como \u002B, así que verificamos ambos formatos
        entry.Detail.Should().Match(d => d.Contains(customerPhone) || d.Contains("\\u002B573208965783"));
        entry.Detail.Should().Contain(customerEmail);
        entry.Detail.Should().Contain(subscriptionId.ToString());
        entry.Detail.Should().Contain(productId.ToString());
        entry.Detail.Should().Contain(clientId.ToString());

        // Verificar que se logueó el número de teléfono (puede estar en formato normal o unicode)
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains(customerPhone) || (v.ToString() ?? string.Empty).Contains("573208965783")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishSubscriptionCreatedEventAsync_WhenEventBridgeFails_ShouldLogError()
    {
        // Arrange
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var productId = 1;
        var clientId = Guid.NewGuid();
        var customerEmail = "test@example.com";
        var customerPhone = "+573208965783";
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        _eventBridgeClient
            .Setup(client => client.PutEventsAsync(It.IsAny<PutEventsRequest>(), cancellationToken))
            .ReturnsAsync(new PutEventsResponse
            {
                Entries = new List<PutEventsResultEntry>
                {
                    new PutEventsResultEntry
                    {
                        ErrorCode = "ErrorCode",
                        ErrorMessage = "Error message"
                    }
                },
                FailedEntryCount = 1
            });

        // Act
        await sut.PublishSubscriptionCreatedEventAsync(
            subscriptionId,
            productId,
            clientId,
            customerEmail,
            customerPhone,
            amount,
            subscribedAtUtc,
            cancellationToken);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishSubscriptionCreatedEventAsync_WhenEventBridgeThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var productId = 1;
        var clientId = Guid.NewGuid();
        var customerEmail = "test@example.com";
        var customerPhone = "+573208965783";
        var amount = 100m;
        var subscribedAtUtc = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        _eventBridgeClient
            .Setup(client => client.PutEventsAsync(It.IsAny<PutEventsRequest>(), cancellationToken))
            .ThrowsAsync(new Exception("EventBridge exception"));

        // Act
        var act = async () => await sut.PublishSubscriptionCreatedEventAsync(
            subscriptionId,
            productId,
            clientId,
            customerEmail,
            customerPhone,
            amount,
            subscribedAtUtc,
            cancellationToken);

        // Assert
        await act.Should().ThrowAsync<Exception>();

        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception while publishing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

