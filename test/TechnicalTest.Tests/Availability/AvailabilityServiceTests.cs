using System.Reflection;
using FluentAssertions;
using Moq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;
using BankBranchEntity = TechnicalTest.Domain.Entities.BankBranch;
using ProductEntity = TechnicalTest.Domain.Entities.Product;
using AvailabilityEntity = TechnicalTest.Domain.Entities.Availability;

namespace TechnicalTest.Tests.Availability;

public partial class AvailabilityServiceTests
{
    private readonly Mock<IAvailabilityRepository> _availabilityRepository = new();
    private readonly Mock<IBankBranchRepository> _bankBranchRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();

    private AvailabilityService CreateSut() =>
        new(
            _availabilityRepository.Object,
            _bankBranchRepository.Object,
            _productRepository.Object);

    private static AvailabilityEntity CreateAvailability(
        int id = 10,
        int bankBranchId = 5,
        int productId = 7)
    {
        var availability = new AvailabilityEntity(bankBranchId, productId);
        typeof(AvailabilityEntity).GetProperty(nameof(AvailabilityEntity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(availability, id);
        return availability;
    }

    private static AvailabilityDto CreateAvailabilityDto(
        int id = 10,
        int bankBranchId = 5,
        int productId = 7) =>
        new(id, bankBranchId, productId);

    private static AvailabilityCreateRequestDto CreateCreateRequest(
        int bankBranchId = 5,
        int productId = 7) =>
        new(bankBranchId, productId);

    private static AvailabilityUpdateRequestDto CreateUpdateRequest(
        int bankBranchId = 8,
        int productId = 12) =>
        new(bankBranchId, productId);

    private static BankBranchEntity CreateBankBranch(int id = 5, string name = "Central", string city = "Madrid")
    {
        var branch = new BankBranchEntity(name, city);
        typeof(BankBranchEntity).GetProperty(nameof(BankBranchEntity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(branch, id);
        return branch;
    }

    private static ProductEntity CreateProduct(int id = 7, string name = "Fondo Dinámico", decimal minimumAmount = 100m, ProductCategory category = ProductCategory.FPV) =>
        new(id, name, minimumAmount, category);

    [Fact]
    public async Task GetAsync_ShouldReturnMappedAvailabilities()
    {
        var sut = CreateSut();
        IReadOnlyCollection<AvailabilityEntity> availability =
        [
            CreateAvailability(1, 10, 20),
            CreateAvailability(2, 11, 21)
        ];
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetAllAsync(cts.Token))
            .ReturnsAsync(availability);

        var result = await sut.GetAsync(cts.Token);

        result.Should().BeEquivalentTo(availability.Select(item => new AvailabilityDto(item.Id, item.BankBranchId, item.ProductId)));
        _availabilityRepository.Verify(repository => repository.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMappedAvailability()
    {
        var sut = CreateSut();
        var availability = CreateAvailability(3, 12, 34);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(availability.Id, cts.Token))
            .ReturnsAsync(availability);

        var result = await sut.GetByIdAsync(availability.Id, cts.Token);

        result.Should().BeEquivalentTo(new AvailabilityDto(availability.Id, availability.BankBranchId, availability.ProductId));
        _availabilityRepository.Verify(repository => repository.GetByIdAsync(availability.Id, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddAvailabilityAndReturnDto()
    {
        var sut = CreateSut();
        var request = CreateCreateRequest(15, 25);
        AvailabilityEntity? addedAvailability = null;
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync(CreateProduct(request.ProductId));
        _availabilityRepository
            .Setup(repository => repository.GetByBranchAndProductAsync(request.BankBranchId, request.ProductId, cts.Token))
            .ReturnsAsync((AvailabilityEntity?)null);
        _availabilityRepository
            .Setup(repository => repository.AddAsync(It.IsAny<AvailabilityEntity>(), cts.Token))
            .Callback<AvailabilityEntity, CancellationToken>((availability, _) => addedAvailability = availability)
            .Returns(Task.CompletedTask);

        var result = await sut.CreateAsync(request, cts.Token);

        addedAvailability.Should().NotBeNull();
        addedAvailability!.BankBranchId.Should().Be(request.BankBranchId);
        addedAvailability.ProductId.Should().Be(request.ProductId);
        result.Should().BeEquivalentTo(CreateAvailabilityDto(addedAvailability.Id, addedAvailability.BankBranchId, addedAvailability.ProductId));
        _availabilityRepository.Verify(repository => repository.AddAsync(addedAvailability, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAvailabilityAndReturnDto()
    {
        var sut = CreateSut();
        var existing = CreateAvailability(4, 30, 40);
        var request = CreateUpdateRequest(50, 60);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync(CreateProduct(request.ProductId));
        _availabilityRepository
            .Setup(repository => repository.GetByBranchAndProductAsync(request.BankBranchId, request.ProductId, cts.Token))
            .ReturnsAsync((AvailabilityEntity?)null);
        _availabilityRepository
            .Setup(repository => repository.UpdateAsync(existing, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.UpdateAsync(existing.Id, request, cts.Token);

        existing.BankBranchId.Should().Be(request.BankBranchId);
        existing.ProductId.Should().Be(request.ProductId);
        result.Should().BeEquivalentTo(new AvailabilityDto(existing.Id, request.BankBranchId, request.ProductId));
        _availabilityRepository.Verify(repository => repository.UpdateAsync(existing, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAvailability()
    {
        var sut = CreateSut();
        var availability = CreateAvailability(8, 101, 202);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(availability.Id, cts.Token))
            .ReturnsAsync(availability);
        _availabilityRepository
            .Setup(repository => repository.DeleteAsync(availability, cts.Token))
            .Returns(Task.CompletedTask);

        await sut.DeleteAsync(availability.Id, cts.Token);

        _availabilityRepository.Verify(repository => repository.DeleteAsync(availability, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _availabilityRepository.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenAvailabilityDoesNotExist()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((AvailabilityEntity?)null);

        var act = async () => await sut.GetByIdAsync(42, cts.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró la disponibilidad solicitada.");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetByIdAsync(5, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _availabilityRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();

        var act = async () => await sut.CreateAsync(null!, cts.Token);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _availabilityRepository.Verify(repository => repository.AddAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDomainException_WhenBankBranchDoesNotExist()
    {
        var sut = CreateSut();
        var request = CreateCreateRequest(70, 80);
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync((BankBranchEntity?)null);

        var act = async () => await sut.CreateAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró la sucursal bancaria con id {request.BankBranchId}.");
        _productRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _availabilityRepository.Verify(repository => repository.AddAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var request = CreateCreateRequest(71, 81);
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync((ProductEntity?)null);

        var act = async () => await sut.CreateAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró el producto con id {request.ProductId}.");
        _availabilityRepository.Verify(repository => repository.AddAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDomainException_WhenAvailabilityAlreadyExists()
    {
        var sut = CreateSut();
        var request = CreateCreateRequest(72, 82);
        var existing = CreateAvailability(9, request.BankBranchId, request.ProductId);
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync(CreateProduct(request.ProductId));
        _availabilityRepository
            .Setup(repository => repository.GetByBranchAndProductAsync(request.BankBranchId, request.ProductId, cts.Token))
            .ReturnsAsync(existing);

        var act = async () => await sut.CreateAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Ya existe una disponibilidad para la sucursal y producto indicados.");
        _availabilityRepository.Verify(repository => repository.AddAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = CreateCreateRequest();

        var act = async () => await sut.CreateAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _availabilityRepository.Verify(repository => repository.AddAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();

        var act = async () => await sut.UpdateAsync(5, null!, cts.Token);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _availabilityRepository.Verify(repository => repository.UpdateAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenAvailabilityDoesNotExist()
    {
        var sut = CreateSut();
        var request = CreateUpdateRequest(90, 91);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((AvailabilityEntity?)null);

        var act = async () => await sut.UpdateAsync(42, request, cts.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró la disponibilidad solicitada.");
        _availabilityRepository.Verify(repository => repository.UpdateAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowDomainException_WhenBankBranchDoesNotExist()
    {
        var sut = CreateSut();
        var existing = CreateAvailability(11, 110, 210);
        var request = CreateUpdateRequest(95, 96);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync((BankBranchEntity?)null);

        var act = async () => await sut.UpdateAsync(existing.Id, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró la sucursal bancaria con id {request.BankBranchId}.");
        _availabilityRepository.Verify(repository => repository.UpdateAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var existing = CreateAvailability(12, 120, 220);
        var request = CreateUpdateRequest(97, 98);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync((ProductEntity?)null);

        var act = async () => await sut.UpdateAsync(existing.Id, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró el producto con id {request.ProductId}.");
        _availabilityRepository.Verify(repository => repository.UpdateAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowDomainException_WhenDuplicateAvailabilityExists()
    {
        var sut = CreateSut();
        var existing = CreateAvailability(13, 130, 230);
        var duplicated = CreateAvailability(14, 131, 231);
        var request = CreateUpdateRequest(duplicated.BankBranchId, duplicated.ProductId);
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(request.BankBranchId, cts.Token))
            .ReturnsAsync(CreateBankBranch(request.BankBranchId));
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync(CreateProduct(request.ProductId));
        _availabilityRepository
            .Setup(repository => repository.GetByBranchAndProductAsync(request.BankBranchId, request.ProductId, cts.Token))
            .ReturnsAsync(duplicated);

        var act = async () => await sut.UpdateAsync(existing.Id, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Ya existe una disponibilidad para la sucursal y producto indicados.");
        _availabilityRepository.Verify(repository => repository.UpdateAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = CreateUpdateRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.UpdateAsync(5, request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _availabilityRepository.Verify(repository => repository.UpdateAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenAvailabilityDoesNotExist()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        _availabilityRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((AvailabilityEntity?)null);

        var act = async () => await sut.DeleteAsync(42, cts.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró la disponibilidad solicitada.");
        _availabilityRepository.Verify(repository => repository.DeleteAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.DeleteAsync(5, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _availabilityRepository.Verify(repository => repository.DeleteAsync(It.IsAny<AvailabilityEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

