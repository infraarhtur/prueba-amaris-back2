using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Application.Interfaces.Repositories;

public interface IScheduleRepository
{
    Task<IReadOnlyCollection<Schedule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Schedule?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(Schedule schedule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Schedule schedule, CancellationToken cancellationToken = default);
}


