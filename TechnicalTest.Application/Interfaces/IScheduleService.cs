using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface IScheduleService
{
    Task<IReadOnlyCollection<ScheduleDto>> GetAsync(CancellationToken cancellationToken);
    Task<ScheduleDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ScheduleDto> CreateAsync(ScheduleCreateRequestDto request, CancellationToken cancellationToken);
    Task<ScheduleDto> UpdateAsync(int id, ScheduleUpdateRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}


