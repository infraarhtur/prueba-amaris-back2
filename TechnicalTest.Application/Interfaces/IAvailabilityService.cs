using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IReadOnlyCollection<AvailabilityDto>> GetAsync(CancellationToken cancellationToken);
    Task<AvailabilityDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<AvailabilityDto> CreateAsync(AvailabilityCreateRequestDto request, CancellationToken cancellationToken);
    Task<AvailabilityDto> UpdateAsync(int id, AvailabilityUpdateRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}



