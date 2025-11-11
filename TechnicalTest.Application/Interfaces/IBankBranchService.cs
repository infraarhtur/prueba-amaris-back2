using TechnicalTest.Application.DTOs;

namespace TechnicalTest.Application.Interfaces;

public interface IBankBranchService
{
    Task<IReadOnlyCollection<BankBranchDto>> GetAsync(CancellationToken cancellationToken);
    Task<BankBranchDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<BankBranchDto> CreateAsync(BankBranchCreateRequestDto request, CancellationToken cancellationToken);
    Task<BankBranchDto> UpdateAsync(int id, BankBranchUpdateRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}


