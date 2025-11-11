using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Subscription>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
}

