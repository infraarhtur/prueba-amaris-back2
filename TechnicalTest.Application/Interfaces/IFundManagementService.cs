using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface IFundManagementService
{
    Task<ClientBalanceDto> GetClientAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FundDto>> GetFundsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken);
    Task<SubscriptionDto> SubscribeAsync(SubscriptionRequestDto request, CancellationToken cancellationToken);
    Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);
}

