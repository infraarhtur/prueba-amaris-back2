using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalTest.Api.Controllers;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Tests.Availability;

public class AvailabilityControllerTests
{
    private readonly Mock<IAvailabilityService> _availabilityService = new();

    private AvailabilityController CreateSut() => new(_availabilityService.Object);

    [Fact]
    public async Task GetAvailabilityAsync_ShouldReturnOkWithAvailabilities()
    {
        var sut = CreateSut();
        IReadOnlyCollection<AvailabilityDto> expected =
        [
            new(1, 2, 3),
            new(4, 5, 6)
        ];
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.GetAsync(cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetAvailabilityAsync(cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        _availabilityService.Verify(service => service.GetAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetAvailabilityByIdAsync_ShouldReturnOkWithAvailability()
    {
        var sut = CreateSut();
        const int availabilityId = 10;
        var expected = new AvailabilityDto(availabilityId, 20, 30);
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.GetByIdAsync(availabilityId, cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetAvailabilityByIdAsync(availabilityId, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        _availabilityService.Verify(service => service.GetByIdAsync(availabilityId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateAvailabilityAsync_ShouldReturnCreatedAvailability()
    {
        var sut = CreateSut();
        var request = new AvailabilityCreateRequestDto(15, 25);
        var created = new AvailabilityDto(7, request.BankBranchId, request.ProductId);
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ReturnsAsync(created);

        var result = await sut.CreateAvailabilityAsync(request, cts.Token);

        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetAvailabilityById");
        createdResult.RouteValues.Should().NotBeNull();
        createdResult.RouteValues!["id"].Should().Be(created.Id);
        createdResult.Value.Should().Be(created);
        _availabilityService.Verify(service => service.CreateAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ShouldReturnUpdatedAvailability()
    {
        var sut = CreateSut();
        const int availabilityId = 32;
        var request = new AvailabilityUpdateRequestDto(40, 50);
        var updated = new AvailabilityDto(availabilityId, request.BankBranchId, request.ProductId);
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.UpdateAsync(availabilityId, request, cts.Token))
            .ReturnsAsync(updated);

        var result = await sut.UpdateAvailabilityAsync(availabilityId, request, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updated);
        _availabilityService.Verify(service => service.UpdateAsync(availabilityId, request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAvailabilityAsync_ShouldReturnNoContent()
    {
        var sut = CreateSut();
        const int availabilityId = 55;
        var cts = new CancellationTokenSource();

        var result = await sut.DeleteAvailabilityAsync(availabilityId, cts.Token);

        result.Should().BeOfType<NoContentResult>();
        _availabilityService.Verify(service => service.DeleteAsync(availabilityId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetAvailabilityByIdAsync_ShouldThrowNotFoundException_WhenServiceThrows()
    {
        var sut = CreateSut();
        const int availabilityId = 77;
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.GetByIdAsync(availabilityId, cts.Token))
            .ThrowsAsync(new NotFoundException("Not found"));

        var act = async () => await sut.GetAvailabilityByIdAsync(availabilityId, cts.Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ShouldThrowNotFoundException_WhenServiceThrows()
    {
        var sut = CreateSut();
        const int availabilityId = 88;
        var request = new AvailabilityUpdateRequestDto(65, 75);
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.UpdateAsync(availabilityId, request, cts.Token))
            .ThrowsAsync(new NotFoundException("Not found"));

        var act = async () => await sut.UpdateAvailabilityAsync(availabilityId, request, cts.Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAvailabilityAsync_ShouldThrowNotFoundException_WhenServiceThrows()
    {
        var sut = CreateSut();
        const int availabilityId = 99;
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.DeleteAsync(availabilityId, cts.Token))
            .ThrowsAsync(new NotFoundException("Not found"));

        var act = async () => await sut.DeleteAvailabilityAsync(availabilityId, cts.Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAvailabilityAsync_ShouldThrowDomainException_WhenServiceThrows()
    {
        var sut = CreateSut();
        var request = new AvailabilityCreateRequestDto(12, 22);
        var cts = new CancellationTokenSource();
        _availabilityService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ThrowsAsync(new DomainException("Invalid request"));

        var act = async () => await sut.CreateAvailabilityAsync(request, cts.Token);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task GetAvailabilityAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _availabilityService
            .Setup(service => service.GetAsync(cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.GetAvailabilityAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAvailabilityByIdAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        const int availabilityId = 41;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _availabilityService
            .Setup(service => service.GetByIdAsync(availabilityId, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.GetAvailabilityByIdAsync(availabilityId, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CreateAvailabilityAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new AvailabilityCreateRequestDto(14, 24);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _availabilityService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.CreateAvailabilityAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        const int availabilityId = 52;
        var request = new AvailabilityUpdateRequestDto(62, 72);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _availabilityService
            .Setup(service => service.UpdateAsync(availabilityId, request, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.UpdateAvailabilityAsync(availabilityId, request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteAvailabilityAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        const int availabilityId = 63;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _availabilityService
            .Setup(service => service.DeleteAsync(availabilityId, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.DeleteAvailabilityAsync(availabilityId, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

