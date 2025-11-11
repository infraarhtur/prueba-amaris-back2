using System.Linq;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Domain.Entities;

namespace TechnicalTest.Infrastructure.Persistence.Repositories;

public class ScheduleRepository(AppDbContext dbContext) : IScheduleRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<IReadOnlyCollection<Schedule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Schedules
            .AsNoTracking()
            .OrderBy(schedule => schedule.AppointmentDate)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Schedule?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(schedule => schedule.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        await _dbContext.Schedules.AddAsync(schedule, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        _dbContext.Schedules.Update(schedule);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        _dbContext.Schedules.Remove(schedule);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}


