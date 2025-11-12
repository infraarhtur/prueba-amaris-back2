using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalTest.Api.Controllers;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Xunit;

namespace TechnicalTest.Tests.Subscriptions;

public class SubscriptionsControllerTests
{
    private readonly Mock<IProductManagementService> _productManagementService = new();

    private SubscriptionsController CreateSut() => new(_productManagementService.Object);

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldReturnOkWithSubscriptions()
    {
        var sut = CreateSut();
        var subscriptions = new[]
        {
            new SubscriptionDto(Guid.NewGuid(), Guid.NewGuid(), 10, 150m, DateTime.UtcNow, null, true),
            new SubscriptionDto(Guid.NewGuid(), Guid.NewGuid(), 20, 250m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, false)
        };
        using var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.GetSubscriptionsAsync(cts.Token))
            .ReturnsAsync(subscriptions);

        var result = await sut.GetSubscriptionsAsync(cts.Token);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(subscriptions);
        _productManagementService.Verify(service => service.GetSubscriptionsAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldReturnCreatedSubscription()
    {
        var sut = CreateSut();
        var request = new SubscriptionRequestDto(30, Guid.NewGuid());
        var subscription = new SubscriptionDto(Guid.NewGuid(), request.ClientId, request.ProductId, 200m, DateTime.UtcNow, null, true);
        using var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.SubscribeAsync(request, cts.Token))
            .ReturnsAsync(subscription);

        var result = await sut.SubscribeAsync(request, cts.Token);

        var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be($"api/subscriptions/{subscription.Id}");
        created.Value.Should().Be(subscription);
        _productManagementService.Verify(service => service.SubscribeAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldReturnOkWithSubscription()
    {
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var subscription = new SubscriptionDto(subscriptionId, Guid.NewGuid(), 40, 250m, DateTime.UtcNow.AddDays(-3), DateTime.UtcNow, false);
        using var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.CancelSubscriptionAsync(subscriptionId, cts.Token))
            .ReturnsAsync(subscription);

        var result = await sut.CancelAsync(subscriptionId, cts.Token);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(subscription);
        _productManagementService.Verify(service => service.CancelSubscriptionAsync(subscriptionId, cts.Token), Times.Once);
    }
}

