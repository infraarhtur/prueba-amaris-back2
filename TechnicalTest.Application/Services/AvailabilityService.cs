using System.Linq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Mappers;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IBankBranchRepository _bankBranchRepository;
    private readonly IProductRepository _productRepository;

    public AvailabilityService(
        IAvailabilityRepository availabilityRepository,
        IBankBranchRepository bankBranchRepository,
        IProductRepository productRepository)
    {
        _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
        _bankBranchRepository = bankBranchRepository ?? throw new ArgumentNullException(nameof(bankBranchRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public async Task<IReadOnlyCollection<AvailabilityDto>> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var availability = await _availabilityRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return availability.Select(item => item.ToDto()).ToArray();
    }

    public async Task<AvailabilityDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var availability = await _availabilityRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                           ?? throw new NotFoundException("No se encontró la disponibilidad solicitada.");

        return availability.ToDto();
    }

    public async Task<AvailabilityDto> CreateAsync(AvailabilityCreateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureBankBranchExistsAsync(request.BankBranchId, cancellationToken).ConfigureAwait(false);
        await EnsureProductExistsAsync(request.ProductId, cancellationToken).ConfigureAwait(false);

        var existing = await _availabilityRepository
            .GetByBranchAndProductAsync(request.BankBranchId, request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            throw new DomainException("Ya existe una disponibilidad para la sucursal y producto indicados.");
        }

        var availability = new Availability(request.BankBranchId, request.ProductId);
        await _availabilityRepository.AddAsync(availability, cancellationToken).ConfigureAwait(false);

        return availability.ToDto();
    }

    public async Task<AvailabilityDto> UpdateAsync(int id, AvailabilityUpdateRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var availability = await _availabilityRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                           ?? throw new NotFoundException("No se encontró la disponibilidad solicitada.");

        await EnsureBankBranchExistsAsync(request.BankBranchId, cancellationToken).ConfigureAwait(false);
        await EnsureProductExistsAsync(request.ProductId, cancellationToken).ConfigureAwait(false);

        var duplicated = await _availabilityRepository
            .GetByBranchAndProductAsync(request.BankBranchId, request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (duplicated is not null && duplicated.Id != availability.Id)
        {
            throw new DomainException("Ya existe una disponibilidad para la sucursal y producto indicados.");
        }

        availability.Update(request.BankBranchId, request.ProductId);
        await _availabilityRepository.UpdateAsync(availability, cancellationToken).ConfigureAwait(false);

        return availability.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var availability = await _availabilityRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
                           ?? throw new NotFoundException("No se encontró la disponibilidad solicitada.");

        await _availabilityRepository.DeleteAsync(availability, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureBankBranchExistsAsync(int bankBranchId, CancellationToken cancellationToken)
    {
        var bankBranch = await _bankBranchRepository.GetByIdAsync(bankBranchId, cancellationToken).ConfigureAwait(false);
        if (bankBranch is null)
        {
            throw new DomainException($"No se encontró la sucursal bancaria con id {bankBranchId}.");
        }
    }

    private async Task EnsureProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            throw new DomainException($"No se encontró el producto con id {productId}.");
        }
    }
}



