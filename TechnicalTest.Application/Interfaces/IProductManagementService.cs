using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface IProductManagementService
{
    Task<ClientBalanceDto> GetClientAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(CancellationToken cancellationToken);
    Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(ProductCreateRequestDto request, CancellationToken cancellationToken);
    Task<ProductDto> UpdateProductAsync(int id, ProductUpdateRequestDto request, CancellationToken cancellationToken);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken);
    Task<SubscriptionDto> SubscribeAsync(SubscriptionRequestDto request, CancellationToken cancellationToken);
    Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken);
}


