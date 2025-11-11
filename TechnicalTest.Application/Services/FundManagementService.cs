using System.Linq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class FundManagementService : IFundManagementService
{
    private readonly IClientRepository _clientRepository;
    private readonly IFundRepository _fundRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public FundManagementService(
        IClientRepository clientRepository,
        IFundRepository fundRepository,
        ISubscriptionRepository subscriptionRepository,
        ITransactionRepository transactionRepository,
        INotificationService notificationService,
        TimeProvider? timeProvider = null)
    {
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
        _fundRepository = fundRepository ?? throw new ArgumentNullException(nameof(fundRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ClientBalanceDto> GetClientAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await GetDefaultClientAsync(cancellationToken).ConfigureAwait(false);
        return client.ToDto();
    }

    public async Task<IReadOnlyCollection<FundDto>> GetFundsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var funds = await _fundRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return funds.Select(fund => fund.ToDto()).ToArray();
    }

    public async Task<IReadOnlyCollection<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await GetDefaultClientAsync(cancellationToken).ConfigureAwait(false);
        var subscriptions = await _subscriptionRepository.GetByClientIdAsync(client.Id, cancellationToken).ConfigureAwait(false);
        return subscriptions.Select(subscription => subscription.ToDto()).ToArray();
    }

    public async Task<IReadOnlyCollection<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await GetDefaultClientAsync(cancellationToken).ConfigureAwait(false);
        var transactions = await _transactionRepository.GetByClientIdAsync(client.Id, cancellationToken).ConfigureAwait(false);
        return transactions.Select(transaction => transaction.ToDto()).OrderByDescending(dto => dto.OccurredAtUtc).ToArray();
    }

    public async Task<SubscriptionDto> SubscribeAsync(SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        var fund = await _fundRepository.GetByIdAsync(request.FundId, cancellationToken).ConfigureAwait(false)
                   ?? throw new DomainException($"No se encontró el fondo con id {request.FundId}.");

        if (request.Amount < fund.MinimumAmount)
        {
            throw new DomainException($"El monto mínimo para el fondo {fund.Name} es {fund.MinimumAmount:C}.");
        }

        var client = await GetDefaultClientAsync(cancellationToken).ConfigureAwait(false);
        var channel = DomainToDtoMapper.ParseChannel(request.NotificationChannel);

        client.UpdateNotificationChannel(channel);
        client.Debit(request.Amount, fund.Name);
        await _clientRepository.UpdateAsync(client, cancellationToken).ConfigureAwait(false);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var subscription = new Subscription(Guid.NewGuid(), client.Id, fund.Id, request.Amount, nowUtc);
        await _subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        var transaction = new Transaction(Guid.NewGuid(), subscription.Id, fund.Id, request.Amount, TransactionType.Subscription, nowUtc);
        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);

        await _notificationService.NotifyAsync(client, fund, channel, cancellationToken).ConfigureAwait(false);

        return subscription.ToDto();
    }

    public async Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false)
                          ?? throw new DomainException("No se encontró la suscripción solicitada.");

        if (!subscription.IsActive)
        {
            throw new DomainException("La suscripción ya se encuentra cancelada.");
        }

        var fund = await _fundRepository.GetByIdAsync(subscription.FundId, cancellationToken).ConfigureAwait(false)
                   ?? throw new DomainException($"No se encontró el fondo con id {subscription.FundId}.");

        var client = await _clientRepository.GetByIdAsync(subscription.ClientId, cancellationToken).ConfigureAwait(false)
                     ?? throw new DomainException("No se encontró el cliente asociado a la suscripción.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        subscription.Cancel(nowUtc);
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

        client.Credit(subscription.Amount);
        await _clientRepository.UpdateAsync(client, cancellationToken).ConfigureAwait(false);

        var transaction = new Transaction(Guid.NewGuid(), subscription.Id, fund.Id, subscription.Amount, TransactionType.Cancellation, nowUtc);
        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);

        return subscription.ToDto();
    }

    private async Task<Client> GetDefaultClientAsync(CancellationToken cancellationToken)
    {
        return await _clientRepository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);
    }
}

