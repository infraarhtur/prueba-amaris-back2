using Microsoft.EntityFrameworkCore;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class ClientRepository(AppDbContext dbContext) : IClientRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(client => client.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        await _dbContext.Clients.AddAsync(client, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        _dbContext.Clients.Update(client);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

