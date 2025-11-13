using Microsoft.EntityFrameworkCore;
using System.Linq;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class ClientRepository(AppDbContext dbContext) : IClientRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private static readonly Guid DefaultClientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string DefaultUserEmail = "demo.client@amaris.com";
    private const string DefaultPhone = "+573001234567";

    public async Task<Client> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == DefaultClientId, cancellationToken)
            .ConfigureAwait(false);

        if (client is not null)
        {
            return client;
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == DefaultUserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            var seedUser = new User(
                DefaultUserId,
                DefaultUserEmail,
                "ZGVmYXVsdF9wYXNzd29yZF9oYXNo",
                "ZGVmYXVsdF9wYXNzd29yZF9zYWx0",
                "Demo Client");

            await _dbContext.Users.AddAsync(seedUser, cancellationToken).ConfigureAwait(false);
        }

        var newClient = new Client(DefaultClientId, DefaultUserId, "Demo", "Client", "Bogota", DefaultUserEmail, DefaultPhone);
        await _dbContext.Clients.AddAsync(newClient, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return newClient;
    }

    public async Task<IReadOnlyCollection<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clients
            .AsNoTracking()
            .OrderBy(client => client.FirstName)
            .ThenBy(client => client.LastName)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

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

    public async Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        _dbContext.Clients.Remove(client);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

