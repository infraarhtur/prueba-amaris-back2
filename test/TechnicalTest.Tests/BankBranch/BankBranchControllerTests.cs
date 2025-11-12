using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalTest.Api.Controllers;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Tests.BankBranch;

public class BankBranchControllerTests
{
    private readonly Mock<IBankBranchService> _bankBranchService = new();

    private bankBranchController CreateSut() => new(_bankBranchService.Object);

    [Fact]
    public async Task GetBankBranchesAsync_ShouldReturnOkWithBranches()
    {
        var sut = CreateSut();
        IReadOnlyCollection<BankBranchDto> expected =
        [
            new(1, "Central", "Madrid"),
            new(2, "North", "Barcelona")
        ];
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.GetAsync(cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetBankBranchesAsync(cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        _bankBranchService.Verify(service => service.GetAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetBankBranchByIdAsync_ShouldReturnOkWithBranch()
    {
        var sut = CreateSut();
        var expected = new BankBranchDto(7, "Central", "Madrid");
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.GetByIdAsync(expected.Id, cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetBankBranchByIdAsync(expected.Id, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        _bankBranchService.Verify(service => service.GetByIdAsync(expected.Id, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateBankBranchAsync_ShouldReturnCreatedBranch()
    {
        var sut = CreateSut();
        var request = new BankBranchCreateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var created = new BankBranchDto(10, request.Name, request.City);
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ReturnsAsync(created);

        var result = await sut.CreateBankBranchAsync(request, cts.Token);

        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetBankBranchById");
        createdResult.RouteValues.Should().NotBeNull();
        createdResult.RouteValues!["id"].Should().Be(created.Id);
        createdResult.Value.Should().Be(created);
        _bankBranchService.Verify(service => service.CreateAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateBankBranchAsync_ShouldReturnUpdatedBranch()
    {
        var sut = CreateSut();
        var id = 5;
        var request = new BankBranchUpdateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var updated = new BankBranchDto(id, request.Name, request.City);
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.UpdateAsync(id, request, cts.Token))
            .ReturnsAsync(updated);

        var result = await sut.UpdateBankBranchAsync(id, request, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updated);
        _bankBranchService.Verify(service => service.UpdateAsync(id, request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteBankBranchAsync_ShouldReturnNoContent()
    {
        var sut = CreateSut();
        var id = 12;
        var cts = new CancellationTokenSource();

        var result = await sut.DeleteBankBranchAsync(id, cts.Token);

        result.Should().BeOfType<NoContentResult>();
        _bankBranchService.Verify(service => service.DeleteAsync(id, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetBankBranchByIdAsync_ShouldThrowNotFoundException_WhenServiceThrows()
    {
        var sut = CreateSut();
        var id = 77;
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.GetByIdAsync(id, cts.Token))
            .ThrowsAsync(new NotFoundException("Not found"));

        var act = async () => await sut.GetBankBranchByIdAsync(id, cts.Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateBankBranchAsync_ShouldThrowNotFoundException_WhenServiceThrows()
    {
        var sut = CreateSut();
        var id = 88;
        var request = new BankBranchUpdateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.UpdateAsync(id, request, cts.Token))
            .ThrowsAsync(new NotFoundException("Not found"));

        var act = async () => await sut.UpdateBankBranchAsync(id, request, cts.Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteBankBranchAsync_ShouldThrowNotFoundException_WhenServiceThrows()
    {
        var sut = CreateSut();
        var id = 91;
        var cts = new CancellationTokenSource();
        _bankBranchService
            .Setup(service => service.DeleteAsync(id, cts.Token))
            .ThrowsAsync(new NotFoundException("Not found"));

        var act = async () => await sut.DeleteBankBranchAsync(id, cts.Token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetBankBranchesAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _bankBranchService
            .Setup(service => service.GetAsync(cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.GetBankBranchesAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetBankBranchByIdAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var id = 3;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _bankBranchService
            .Setup(service => service.GetByIdAsync(id, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.GetBankBranchByIdAsync(id, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CreateBankBranchAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new BankBranchCreateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _bankBranchService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.CreateBankBranchAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateBankBranchAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var id = 11;
        var request = new BankBranchUpdateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _bankBranchService
            .Setup(service => service.UpdateAsync(id, request, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.UpdateBankBranchAsync(id, request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteBankBranchAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var id = 15;
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _bankBranchService
            .Setup(service => service.DeleteAsync(id, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await sut.DeleteBankBranchAsync(id, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}


