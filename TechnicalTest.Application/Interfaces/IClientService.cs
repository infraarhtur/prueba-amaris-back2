using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface IClientService
{
    Task<IReadOnlyCollection<ClientDto>> GetAsync(CancellationToken cancellationToken);
    Task<ClientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ClientDto> CreateAsync(ClientCreateRequestDto request, CancellationToken cancellationToken);
    Task<ClientDto> UpdateAsync(Guid id, ClientUpdateRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
