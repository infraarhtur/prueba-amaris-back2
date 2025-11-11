using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<IReadOnlyCollection<Transaction>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
}

