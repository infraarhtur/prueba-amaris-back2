using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface IFundRepository
{
    Task<Fund?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Fund>> GetAllAsync(CancellationToken cancellationToken = default);
}

