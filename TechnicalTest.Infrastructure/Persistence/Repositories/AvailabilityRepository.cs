using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class AvailabilityRepository(AppDbContext dbContext) : IAvailabilityRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyCollection<Availability>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Availabilities
            .AsNoTracking()
            .OrderBy(availability => availability.BankBranchId)
            .ThenBy(availability => availability.ProductId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Availability?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Availabilities
            .AsNoTracking()
            .FirstOrDefaultAsync(availability => availability.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Availability?> GetByBranchAndProductAsync(int bankBranchId, int productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Availabilities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                availability => availability.BankBranchId == bankBranchId && availability.ProductId == productId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(availability);

        await _dbContext.Availabilities.AddAsync(availability, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(availability);

        _dbContext.Availabilities.Update(availability);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(availability);

        _dbContext.Availabilities.Remove(availability);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}



