using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository(AppDbContext dbContext) : ISubscriptionRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(subscription => subscription.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Subscription>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.ClientId == clientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return subscriptions;
    }

    public async Task<IReadOnlyCollection<Subscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .OrderBy(subscription => subscription.SubscribedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return subscriptions;
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        await _dbContext.Subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        _dbContext.Subscriptions.Update(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

