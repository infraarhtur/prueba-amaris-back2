using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Moq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Exceptions;
using Xunit;
using DomainSchedule = TechnicalTest.Domain.Entities.Schedule;
using BankBranchEntity = TechnicalTest.Domain.Entities.BankBranch;
using ClientEntity = TechnicalTest.Domain.Entities.Client;

namespace TechnicalTest.Tests.Schedule;

public partial class ScheduleServiceTests
{
    private static readonly Guid DefaultClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UpdatedClientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime DefaultAppointmentDate = new(2025, 01, 02, 03, 04, 05, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAppointmentDate = new(2025, 02, 03, 04, 05, 06, DateTimeKind.Utc);

    private readonly Mock<IScheduleRepository> _scheduleRepository = new();
    private readonly Mock<IBankBranchRepository> _bankBranchRepository = new();
    private readonly Mock<IClientRepository> _clientRepository = new();

    private ScheduleService CreateSut() =>
        new(
            _scheduleRepository.Object,
            _bankBranchRepository.Object,
            _clientRepository.Object);

    private static DomainSchedule CreateSchedule(
        int id = 10,
        int bankBranchId = 5,
        Guid? clientId = null,
        DateTime? appointmentDate = null)
    {
        var schedule = new DomainSchedule(
            bankBranchId,
            clientId ?? DefaultClientId,
            appointmentDate ?? DefaultAppointmentDate);

        typeof(DomainSchedule)
            .GetProperty(nameof(DomainSchedule.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(schedule, id);

        return schedule;
    }

    private static ScheduleCreateRequestDto CreateCreateRequest(
        int bankBranchId = 5,
        Guid? clientId = null,
        DateTime? appointmentDate = null) =>
        new(
            bankBranchId,
            clientId ?? DefaultClientId,
            appointmentDate ?? DefaultAppointmentDate);

    private static ScheduleUpdateRequestDto CreateUpdateRequest(
        int bankBranchId = 6,
        Guid? clientId = null,
        DateTime? appointmentDate = null) =>
        new(
            bankBranchId,
            clientId ?? UpdatedClientId,
            appointmentDate ?? UpdatedAppointmentDate);

    private static BankBranchEntity CreateBankBranch(int id = 10, string name = "Central", string city = "Madrid")
    {
        var branch = new BankBranchEntity(name, city);
        typeof(BankBranchEntity)
            .GetProperty(nameof(BankBranchEntity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(branch, id);
        return branch;
    }

    private static ClientEntity CreateClient(Guid? id = null, Guid? userId = null)
        => new(
            id ?? DefaultClientId,
            userId ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Alice",
            "Smith",
            "Madrid",
            "alice.smith@example.com",
            "+1234567890");

    [Fact]
    public async Task GetAsync_ShouldReturnMappedSchedules()
    {
        var schedules = new[]
        {
            CreateSchedule(1, 10),
            CreateSchedule(2, 11, UpdatedClientId, UpdatedAppointmentDate)
        };
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetAllAsync(cancellationToken))
            .ReturnsAsync(schedules.ToImmutableArray());

        var sut = CreateSut();

        var result = await sut.GetAsync(cancellationToken);

        result.Should()
            .BeEquivalentTo(
                schedules,
                options => options.ComparingByMembers<DomainSchedule>());
        _scheduleRepository.Verify(repository => repository.GetAllAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = async () => await sut.GetAsync(cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _scheduleRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMappedSchedule()
    {
        const int scheduleId = 15;
        var schedule = CreateSchedule(scheduleId, 20);
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync(schedule);

        var sut = CreateSut();

        var result = await sut.GetByIdAsync(scheduleId, cancellationToken);

        result.Should().BeEquivalentTo(schedule, options => options.ComparingByMembers<DomainSchedule>());
        _scheduleRepository.Verify(repository => repository.GetByIdAsync(scheduleId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenScheduleDoesNotExist_ShouldThrowNotFound()
    {
        const int scheduleId = 25;
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync((DomainSchedule?)null);

        var sut = CreateSut();

        var act = async () => await sut.GetByIdAsync(scheduleId, cancellationToken);

        await act.Should().ThrowExactlyAsync<NotFoundException>()
            .WithMessage("No se encontró la cita solicitada.");
        _scheduleRepository.Verify(repository => repository.GetByIdAsync(scheduleId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        const int scheduleId = 35;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = async () => await sut.GetByIdAsync(scheduleId, cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _scheduleRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddScheduleAndReturnDto()
    {
        var request = CreateCreateRequest();
        var cancellationToken = CancellationToken.None;
        var sut = CreateSut();

        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cancellationToken))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(request.ClientId, cancellationToken))
            .ReturnsAsync(CreateClient(request.ClientId));

        var result = await sut.CreateAsync(request, cancellationToken);

        result.Should().BeEquivalentTo(
            request,
            options => options.ExcludingMissingMembers());
        _scheduleRepository.Verify(repository =>
            repository.AddAsync(It.Is<DomainSchedule>(schedule =>
                schedule.BankBranchId == request.BankBranchId &&
                schedule.ClientId == request.ClientId &&
                schedule.AppointmentDate == request.AppointmentDate),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenBankBranchDoesNotExist_ShouldThrowDomainException()
    {
        var request = CreateCreateRequest();
        var cancellationToken = CancellationToken.None;
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cancellationToken))
            .ReturnsAsync((BankBranchEntity?)null);

        var sut = CreateSut();

        var act = async () => await sut.CreateAsync(request, cancellationToken);

        await act.Should().ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró la sucursal bancaria con id {request.BankBranchId}.");
        _scheduleRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenClientDoesNotExist_ShouldThrowDomainException()
    {
        var request = CreateCreateRequest();
        var cancellationToken = CancellationToken.None;
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cancellationToken))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(request.ClientId, cancellationToken))
            .ReturnsAsync((ClientEntity?)null);

        var sut = CreateSut();

        var act = async () => await sut.CreateAsync(request, cancellationToken);

        await act.Should().ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró el cliente con id {request.ClientId}.");
        _scheduleRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.CreateAsync(null!, CancellationToken.None);

        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task CreateAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = async () => await sut.CreateAsync(CreateCreateRequest(), cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _scheduleRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateScheduleAndReturnDto()
    {
        const int scheduleId = 50;
        var existingSchedule = CreateSchedule(scheduleId, 15);
        var request = CreateUpdateRequest();
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync(existingSchedule);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cancellationToken))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(request.ClientId, cancellationToken))
            .ReturnsAsync(CreateClient(request.ClientId));

        var sut = CreateSut();

        var result = await sut.UpdateAsync(scheduleId, request, cancellationToken);

        result.Should().BeEquivalentTo(existingSchedule, options => options.ComparingByMembers<DomainSchedule>());
        _scheduleRepository.Verify(repository => repository.UpdateAsync(existingSchedule, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenScheduleDoesNotExist_ShouldThrowNotFound()
    {
        const int scheduleId = 60;
        var request = CreateUpdateRequest();
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync((DomainSchedule?)null);

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(scheduleId, request, cancellationToken);

        await act.Should().ThrowExactlyAsync<NotFoundException>()
            .WithMessage("No se encontró la cita solicitada.");
        _scheduleRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenBankBranchDoesNotExist_ShouldThrowDomainException()
    {
        const int scheduleId = 70;
        var existingSchedule = CreateSchedule(scheduleId, 10);
        var request = CreateUpdateRequest();
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync(existingSchedule);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cancellationToken))
            .ReturnsAsync((BankBranchEntity?)null);

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(scheduleId, request, cancellationToken);

        await act.Should().ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró la sucursal bancaria con id {request.BankBranchId}.");
        _scheduleRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenClientDoesNotExist_ShouldThrowDomainException()
    {
        const int scheduleId = 80;
        var existingSchedule = CreateSchedule(scheduleId, 10);
        var request = CreateUpdateRequest();
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync(existingSchedule);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cancellationToken))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(request.ClientId, cancellationToken))
            .ReturnsAsync((ClientEntity?)null);

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(scheduleId, request, cancellationToken);

        await act.Should().ThrowExactlyAsync<DomainException>()
            .WithMessage($"No se encontró el cliente con id {request.ClientId}.");
        _scheduleRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(1, null!, CancellationToken.None);

        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task UpdateAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        const int scheduleId = 90;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(scheduleId, CreateUpdateRequest(), cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _scheduleRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSchedule()
    {
        const int scheduleId = 100;
        var existingSchedule = CreateSchedule(scheduleId, 15);
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync(existingSchedule);

        var sut = CreateSut();

        await sut.DeleteAsync(scheduleId, cancellationToken);

        _scheduleRepository.Verify(repository => repository.DeleteAsync(existingSchedule, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenScheduleDoesNotExist_ShouldThrowNotFound()
    {
        const int scheduleId = 110;
        var cancellationToken = CancellationToken.None;
        _scheduleRepository
            .Setup(repository => repository.GetByIdAsync(scheduleId, cancellationToken))
            .ReturnsAsync((DomainSchedule?)null);

        var sut = CreateSut();

        var act = async () => await sut.DeleteAsync(scheduleId, cancellationToken);

        await act.Should().ThrowExactlyAsync<NotFoundException>()
            .WithMessage("No se encontró la cita solicitada.");
        _scheduleRepository.Verify(repository => repository.DeleteAsync(It.IsAny<DomainSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        const int scheduleId = 120;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = async () => await sut.DeleteAsync(scheduleId, cts.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>();
        _scheduleRepository.VerifyNoOtherCalls();
    }
}

