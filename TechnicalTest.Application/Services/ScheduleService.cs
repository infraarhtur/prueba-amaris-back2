using System.Linq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IBankBranchRepository _bankBranchRepository;
    private readonly IClientRepository _clientRepository;

    public ScheduleService(
        IScheduleRepository scheduleRepository,
        IBankBranchRepository bankBranchRepository,
        IClientRepository clientRepository)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _bankBranchRepository = bankBranchRepository ?? throw new ArgumentNullException(nameof(bankBranchRepository));
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
    }

    public async Task<IReadOnlyCollection<ScheduleDto>> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schedules = await _scheduleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return schedules.Select(schedule => schedule.ToDto()).ToArray();
    }

    public async Task<ScheduleDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                       ?? throw new NotFoundException("No se encontró la cita solicitada.");

        return schedule.ToDto();
    }

    public async Task<ScheduleDto> CreateAsync(ScheduleCreateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureBankBranchExistsAsync(request.BankBranchId, cancellationToken).ConfigureAwait(false);
        await EnsureClientExistsAsync(request.ClientId, cancellationToken).ConfigureAwait(false);

        var schedule = new Schedule(request.BankBranchId, request.ClientId, request.AppointmentDate);
        await _scheduleRepository.AddAsync(schedule, cancellationToken).ConfigureAwait(false);

        return schedule.ToDto();
    }

    public async Task<ScheduleDto> UpdateAsync(int id, ScheduleUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                       ?? throw new NotFoundException("No se encontró la cita solicitada.");

        await EnsureBankBranchExistsAsync(request.BankBranchId, cancellationToken).ConfigureAwait(false);
        await EnsureClientExistsAsync(request.ClientId, cancellationToken).ConfigureAwait(false);

        schedule.Update(request.BankBranchId, request.ClientId, request.AppointmentDate);
        await _scheduleRepository.UpdateAsync(schedule, cancellationToken).ConfigureAwait(false);

        return schedule.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                       ?? throw new NotFoundException("No se encontró la cita solicitada.");

        await _scheduleRepository.DeleteAsync(schedule, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureBankBranchExistsAsync(int bankBranchId, CancellationToken cancellationToken)
    {
        var bankBranch = await _bankBranchRepository.GetByIdAsync(bankBranchId, cancellationToken).ConfigureAwait(false);
        if (bankBranch is null)
        {
            throw new DomainException($"No se encontró la sucursal bancaria con id {bankBranchId}.");
        }
    }

    private async Task EnsureClientExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            throw new DomainException($"No se encontró el cliente con id {clientId}.");
        }
    }
}


