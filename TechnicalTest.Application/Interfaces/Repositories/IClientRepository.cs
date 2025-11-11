using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
}

