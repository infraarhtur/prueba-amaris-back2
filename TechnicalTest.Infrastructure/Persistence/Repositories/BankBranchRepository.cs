using Microsoft.EntityFrameworkCore;
using System.Linq;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class BankBranchRepository(AppDbContext dbContext) : IBankBranchRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyCollection<BankBranch>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.BankBranches
            .AsNoTracking()
            .OrderBy(branch => branch.Name)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BankBranch?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BankBranches
            .AsNoTracking()
            .FirstOrDefaultAsync(branch => branch.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(BankBranch bankBranch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bankBranch);

        await _dbContext.BankBranches.AddAsync(bankBranch, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(BankBranch bankBranch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bankBranch);

        _dbContext.BankBranches.Update(bankBranch);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(BankBranch bankBranch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bankBranch);

        _dbContext.BankBranches.Remove(bankBranch);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}


