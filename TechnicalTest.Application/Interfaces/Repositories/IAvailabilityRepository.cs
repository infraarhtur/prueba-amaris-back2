using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface IAvailabilityRepository
{
    Task<IReadOnlyCollection<Availability>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Availability?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Availability?> GetByBranchAndProductAsync(int bankBranchId, int productId, CancellationToken cancellationToken = default);
    Task AddAsync(Availability availability, CancellationToken cancellationToken = default);
    Task UpdateAsync(Availability availability, CancellationToken cancellationToken = default);
    Task DeleteAsync(Availability availability, CancellationToken cancellationToken = default);
}



