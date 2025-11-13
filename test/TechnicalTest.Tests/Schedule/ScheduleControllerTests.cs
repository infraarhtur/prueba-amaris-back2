using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalTest.Api.Controllers;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Xunit;

namespace TechnicalTest.Tests.Schedule;

public class ScheduleControllerTests
{
    private readonly Mock<IScheduleService> _scheduleService = new();

    private ScheduleController CreateSut() => new(_scheduleService.Object);

    [Fact]
    public async Task GetSchedulesAsync_ShouldReturnOkWithSchedules()
    {
        var sut = CreateSut();
        var expected = new[]
        {
            new ScheduleDto(1, 10, Guid.NewGuid(), DateTime.UtcNow),
            new ScheduleDto(2, 11, Guid.NewGuid(), DateTime.UtcNow.AddDays(1))
        };
        var cts = new CancellationTokenSource();
        _scheduleService
            .Setup(service => service.GetAsync(cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetSchedulesAsync(cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        _scheduleService.Verify(service => service.GetAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByIdAsync_ShouldReturnOkWithSchedule()
    {
        var sut = CreateSut();
        const int scheduleId = 5;
        var expected = new ScheduleDto(scheduleId, 12, Guid.NewGuid(), DateTime.UtcNow);
        var cts = new CancellationTokenSource();
        _scheduleService
            .Setup(service => service.GetByIdAsync(scheduleId, cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetScheduleByIdAsync(scheduleId, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        _scheduleService.Verify(service => service.GetByIdAsync(scheduleId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateScheduleAsync_ShouldReturnCreatedAtRoute()
    {
        var sut = CreateSut();
        var request = new ScheduleCreateRequestDto(10, Guid.NewGuid(), DateTime.UtcNow);
        var created = new ScheduleDto(20, request.BankBranchId, request.ClientId, request.AppointmentDate);
        var cts = new CancellationTokenSource();
        _scheduleService
            .Setup(service => service.CreateAsync(request, cts.Token))
            .ReturnsAsync(created);

        var result = await sut.CreateScheduleAsync(request, cts.Token);

        var createdAtRoute = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdAtRoute.RouteName.Should().Be("GetScheduleById");
        createdAtRoute.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(created.Id);
        createdAtRoute.Value.Should().Be(created);
        _scheduleService.Verify(service => service.CreateAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ShouldReturnOkWithUpdatedSchedule()
    {
        var sut = CreateSut();
        const int scheduleId = 15;
        var request = new ScheduleUpdateRequestDto(16, Guid.NewGuid(), DateTime.UtcNow);
        var updated = new ScheduleDto(scheduleId, request.BankBranchId, request.ClientId, request.AppointmentDate);
        var cts = new CancellationTokenSource();
        _scheduleService
            .Setup(service => service.UpdateAsync(scheduleId, request, cts.Token))
            .ReturnsAsync(updated);

        var result = await sut.UpdateScheduleAsync(scheduleId, request, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updated);
        _scheduleService.Verify(service => service.UpdateAsync(scheduleId, request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteScheduleAsync_ShouldReturnNoContent()
    {
        var sut = CreateSut();
        const int scheduleId = 25;
        var cts = new CancellationTokenSource();
        _scheduleService
            .Setup(service => service.DeleteAsync(scheduleId, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.DeleteScheduleAsync(scheduleId, cts.Token);

        result.Should().BeOfType<NoContentResult>();
        _scheduleService.Verify(service => service.DeleteAsync(scheduleId, cts.Token), Times.Once);
    }
}

