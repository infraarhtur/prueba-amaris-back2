using System.Linq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class BankBranchService(IBankBranchRepository bankBranchRepository) : IBankBranchService
{
    private readonly IBankBranchRepository _bankBranchRepository = bankBranchRepository ?? throw new ArgumentNullException(nameof(bankBranchRepository));

    public async Task<IReadOnlyCollection<BankBranchDto>> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var branches = await _bankBranchRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return branches.Select(branch => branch.ToDto()).ToArray();
    }

    public async Task<BankBranchDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var branch = await _bankBranchRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                     ?? throw new NotFoundException("No se encontró la sucursal solicitada.");

        return branch.ToDto();
    }

    public async Task<BankBranchDto> CreateAsync(BankBranchCreateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var branch = new BankBranch(request.Name, request.City);
        await _bankBranchRepository.AddAsync(branch, cancellationToken).ConfigureAwait(false);

        return branch.ToDto();
    }

    public async Task<BankBranchDto> UpdateAsync(int id, BankBranchUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var branch = await _bankBranchRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                     ?? throw new NotFoundException("No se encontró la sucursal solicitada.");

        branch.Update(request.Name, request.City);
        await _bankBranchRepository.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);

        return branch.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var branch = await _bankBranchRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                     ?? throw new NotFoundException("No se encontró la sucursal solicitada.");

        await _bankBranchRepository.DeleteAsync(branch, cancellationToken).ConfigureAwait(false);
    }
}


