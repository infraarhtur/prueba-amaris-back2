using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalTest.Api.Controllers;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Xunit;

namespace TechnicalTest.Tests.Client;

public class ClientControllerTests
{
    private readonly Mock<IProductManagementService> _productManagementService = new();
    private readonly Mock<IClientService> _clientService = new();

    private ClientController CreateSut() =>
        new(_productManagementService.Object, _clientService.Object);

    [Fact]
    public async Task GetDefaultClientBalanceAsync_ShouldReturnOkWithBalance()
    {
        var sut = CreateSut();
        var expected = new ClientBalanceDto(Guid.NewGuid(), 150.75m, "email");
        var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.GetClientAsync(cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetDefaultClientBalanceAsync(cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        _productManagementService.Verify(service => service.GetClientAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetClientsAsync_ShouldReturnOkWithClients()
    {
        var sut = CreateSut();
        var expected = new List<ClientDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Alice", "Smith", "Madrid", "alice@example.com", 100m, "email", DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "Bob", "Jones", "Barcelona", "bob@example.com", 200m, "sms", DateTime.UtcNow)
        };
        var cts = new CancellationTokenSource();
        _clientService
            .Setup(service => service.GetAsync(cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetClientsAsync(cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        _clientService.Verify(service => service.GetAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetClientByIdAsync_ShouldReturnOkWithClient()
    {
        var sut = CreateSut();
        var clientId = Guid.NewGuid();
        var expected = new ClientDto(clientId, Guid.NewGuid(), "Alice", "Smith", "Madrid", "alice@example.com", 100m, "email", DateTime.UtcNow);
        var cts = new CancellationTokenSource();
        _clientService
            .Setup(service => service.GetByIdAsync(clientId, cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetClientByIdAsync(clientId, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        _clientService.Verify(service => service.GetByIdAsync(clientId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateClientAsync_ShouldReturnCreatedClient()
    {
        var sut = CreateSut();
        var request = new ClientCreateRequestDto(Guid.NewGuid(), "Alice", "Smith", "Madrid", "alice@example.com", 100m, "email");
        var created = new ClientDto(Guid.NewGuid(), request.UserId, request.FirstName, request.LastName, request.City, request.Email, request.Balance ?? 0m, request.NotificationChannel ?? "email", DateTime.UtcNow);
        var cts = new CancellationTokenSource();
        _clientService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ReturnsAsync(created);

        var result = await sut.CreateClientAsync(request, cts.Token);

        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetClientById");
        createdResult.RouteValues.Should().NotBeNull();
        createdResult.RouteValues!["id"].Should().Be(created.Id);
        createdResult.Value.Should().Be(created);
        _clientService.Verify(service => service.CreateAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldReturnUpdatedClient()
    {
        var sut = CreateSut();
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new ClientUpdateRequestDto("Alice", "Smith", "Madrid", "alice@example.com", 150m, "sms", userId);
        var updated = new ClientDto(clientId, userId, request.FirstName, request.LastName, request.City, request.Email, request.Balance ?? 0m, request.NotificationChannel ?? "email", DateTime.UtcNow);
        var cts = new CancellationTokenSource();
        _clientService
            .Setup(service => service.UpdateAsync(clientId, request, cts.Token))
            .ReturnsAsync(updated);

        var result = await sut.UpdateClientAsync(clientId, request, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updated);
        _clientService.Verify(service => service.UpdateAsync(clientId, request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteClientAsync_ShouldReturnNoContent()
    {
        var sut = CreateSut();
        var clientId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        var result = await sut.DeleteClientAsync(clientId, cts.Token);

        result.Should().BeOfType<NoContentResult>();
        _clientService.Verify(service => service.DeleteAsync(clientId, cts.Token), Times.Once);
    }
}

