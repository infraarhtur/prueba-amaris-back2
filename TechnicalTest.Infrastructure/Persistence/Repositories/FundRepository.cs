using Microsoft.EntityFrameworkCore;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class FundRepository(AppDbContext dbContext) : IFundRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Fund?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Funds
            .AsNoTracking()
            .FirstOrDefaultAsync(fund => fund.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Fund>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var funds = await _dbContext.Funds
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return funds;
    }
}

