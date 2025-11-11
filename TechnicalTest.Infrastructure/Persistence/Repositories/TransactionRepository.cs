using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class TransactionRepository(AppDbContext dbContext) : ITransactionRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyCollection<Transaction>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var subscriptionIdsQuery = _dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.ClientId == clientId)
            .Select(subscription => subscription.Id);

        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => subscriptionIdsQuery.Contains(transaction.SubscriptionId))
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return transactions;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await _dbContext.Transactions.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

