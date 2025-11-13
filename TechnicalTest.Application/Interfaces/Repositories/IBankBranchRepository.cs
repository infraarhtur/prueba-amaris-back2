using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface IBankBranchRepository
{
    Task<IReadOnlyCollection<BankBranch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BankBranch?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(BankBranch bankBranch, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankBranch bankBranch, CancellationToken cancellationToken = default);
    Task DeleteAsync(BankBranch bankBranch, CancellationToken cancellationToken = default);
}


