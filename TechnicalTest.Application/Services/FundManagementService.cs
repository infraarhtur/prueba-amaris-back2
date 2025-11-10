using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Data;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class FundManagementService : IFundManagementService
{
    private readonly List<Fund> _funds;
    private readonly Client _client;
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<Transaction> _transactions = [];
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _syncSemaphore = new(1, 1);

    public FundManagementService(INotificationService notificationService, TimeProvider? timeProvider = null)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _funds = FundCatalog.GetDefaultFunds().ToList();
        _client = new Client(Guid.NewGuid());
    }

    public async Task<ClientBalanceDto> GetClientAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _syncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _client.ToDto();
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<FundDto>> GetFundsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _syncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _funds.Select(f => f.ToDto()).ToArray();
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _syncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _subscriptions.Select(s => s.ToDto()).ToArray();
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _syncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _transactions.Select(t => t.ToDto()).OrderByDescending(t => t.OccurredAtUtc).ToArray();
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public async Task<SubscriptionDto> SubscribeAsync(SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        await _syncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var fund = _funds.FirstOrDefault(f => f.Id == request.FundId)
                       ?? throw new DomainException($"No se encontró el fondo con id {request.FundId}.");

            if (request.Amount < fund.MinimumAmount)
            {
                throw new DomainException($"El monto mínimo para el fondo {fund.Name} es {fund.MinimumAmount:C}.");
            }

            var channel = DomainToDtoMapper.ParseChannel(request.NotificationChannel);

            _client.UpdateNotificationChannel(channel);
            _client.Debit(request.Amount, fund.Name);

            var subscription = new Subscription(Guid.NewGuid(), fund.Id, request.Amount, _timeProvider.GetUtcNow().UtcDateTime);
            _subscriptions.Add(subscription);

            var transaction = new Transaction(Guid.NewGuid(), subscription.Id, fund.Id, request.Amount, TransactionType.Subscription, _timeProvider.GetUtcNow().UtcDateTime);
            _transactions.Add(transaction);

            await _notificationService.NotifyAsync(_client, fund, channel, cancellationToken).ConfigureAwait(false);

            return subscription.ToDto();
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    public async Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _syncSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId)
                               ?? throw new DomainException("No se encontró la suscripción solicitada.");

            if (!subscription.IsActive)
            {
                throw new DomainException("La suscripción ya se encuentra cancelada.");
            }

            var fund = _funds.First(f => f.Id == subscription.FundId);

            subscription.Cancel(_timeProvider.GetUtcNow().UtcDateTime);
            _client.Credit(subscription.Amount);

            var transaction = new Transaction(Guid.NewGuid(), subscription.Id, fund.Id, subscription.Amount, TransactionType.Cancellation, _timeProvider.GetUtcNow().UtcDateTime);
            _transactions.Add(transaction);

            return subscription.ToDto();
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }
}

