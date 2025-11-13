using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class ClientService(IClientRepository clientRepository) : IClientService
{
    private readonly IClientRepository _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));

    public async Task<IReadOnlyCollection<ClientDto>> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clients = await _clientRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return clients.Select(client => client.ToClientDto()).ToArray();
    }

    public async Task<ClientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                     ?? throw new NotFoundException("No se encontró el cliente solicitado.");
        return client.ToClientDto();
    }

    public async Task<ClientDto> CreateAsync(ClientCreateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var channel = DomainToDtoMapper.ParseChannelOrDefault(request.NotificationChannel, NotificationChannel.Email);
        var balance = request.Balance ?? Client.InitialBalance;
        var client = new Client(
            Guid.NewGuid(),
            request.UserId,
            request.FirstName,
            request.LastName,
            request.City,
            request.Email,
            balance,
            channel);

        await _clientRepository.AddAsync(client, cancellationToken).ConfigureAwait(false);
        return client.ToClientDto();
    }

    public async Task<ClientDto> UpdateAsync(Guid id, ClientUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await _clientRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                       ?? throw new NotFoundException("No se encontró el cliente solicitado.");

        existing.UpdatePersonalInfo(request.FirstName, request.LastName, request.City, request.Email);

        if (request.UserId.HasValue)
        {
            existing.AssignUser(request.UserId.Value);
        }

        if (request.Balance.HasValue)
        {
            existing.UpdateBalance(request.Balance.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.NotificationChannel))
        {
            var channel = DomainToDtoMapper.ParseChannel(request.NotificationChannel);
            existing.UpdateNotificationChannel(channel);
        }

        await _clientRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return existing.ToClientDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await _clientRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                       ?? throw new NotFoundException("No se encontró el cliente solicitado.");

        await _clientRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);
    }
}
