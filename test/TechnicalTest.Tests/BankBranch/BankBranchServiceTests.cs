using System.Reflection;
using FluentAssertions;
using Moq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Exceptions;
using BankBranchEntity = TechnicalTest.Domain.Entities.BankBranch;

namespace TechnicalTest.Tests.BankBranch;

public class BankBranchServiceTests
{
    private readonly Mock<IBankBranchRepository> _bankBranchRepository = new();

    private BankBranchService CreateSut() => new(_bankBranchRepository.Object);

    [Fact]
    public async Task GetAsync_ShouldReturnMappedBranches()
    {
        var sut = CreateSut();
        IReadOnlyCollection<BankBranchEntity> branches =
        [
            CreateBankBranch(1, "Central", "Madrid"),
            CreateBankBranch(2, "North", "Barcelona")
        ];
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetAllAsync(cts.Token))
            .ReturnsAsync(branches);

        var result = await sut.GetAsync(cts.Token);

        result.Should().BeEquivalentTo(branches.Select(branch => new BankBranchDto(branch.Id, branch.Name, branch.City)));
        _bankBranchRepository.Verify(repository => repository.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMappedBranch()
    {
        var sut = CreateSut();
        var branch = CreateBankBranch(7, "Central", "Madrid");
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(branch.Id, cts.Token))
            .ReturnsAsync(branch);

        var result = await sut.GetByIdAsync(branch.Id, cts.Token);

        result.Should().BeEquivalentTo(new BankBranchDto(branch.Id, branch.Name, branch.City));
        _bankBranchRepository.Verify(repository => repository.GetByIdAsync(branch.Id, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddBranchAndReturnDto()
    {
        var sut = CreateSut();
        var request = new BankBranchCreateRequestDto
        {
            Name = " Central ",
            City = " Madrid "
        };
        BankBranchEntity? addedBranch = null;
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.AddAsync(It.IsAny<BankBranchEntity>(), cts.Token))
            .Callback<BankBranchEntity, CancellationToken>((branch, _) => addedBranch = branch)
            .Returns(Task.CompletedTask);

        var result = await sut.CreateAsync(request, cts.Token);

        addedBranch.Should().NotBeNull();
        addedBranch!.Name.Should().Be("Central");
        addedBranch.City.Should().Be("Madrid");
        result.Should().BeEquivalentTo(new BankBranchDto(addedBranch.Id, addedBranch.Name, addedBranch.City));
        _bankBranchRepository.Verify(repository => repository.AddAsync(addedBranch, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBranchAndReturnDto()
    {
        var sut = CreateSut();
        var branch = CreateBankBranch(11, "Old", "Sevilla");
        var request = new BankBranchUpdateRequestDto
        {
            Name = " Central ",
            City = " Madrid "
        };
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(branch.Id, cts.Token))
            .ReturnsAsync(branch);
        _bankBranchRepository
            .Setup(repository => repository.UpdateAsync(branch, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.UpdateAsync(branch.Id, request, cts.Token);

        branch.Name.Should().Be("Central");
        branch.City.Should().Be("Madrid");
        result.Should().BeEquivalentTo(new BankBranchDto(branch.Id, branch.Name, branch.City));
        _bankBranchRepository.Verify(repository => repository.UpdateAsync(branch, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveBranch()
    {
        var sut = CreateSut();
        var branch = CreateBankBranch(15, "Central", "Madrid");
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(branch.Id, cts.Token))
            .ReturnsAsync(branch);
        _bankBranchRepository
            .Setup(repository => repository.DeleteAsync(branch, cts.Token))
            .Returns(Task.CompletedTask);

        await sut.DeleteAsync(branch.Id, cts.Token);

        _bankBranchRepository.Verify(repository => repository.DeleteAsync(branch, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenBranchDoesNotExist()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((BankBranchEntity?)null);

        var act = async () => await sut.GetByIdAsync(42, cts.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró la sucursal solicitada.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFoundException_WhenBranchDoesNotExist()
    {
        var sut = CreateSut();
        var request = new BankBranchUpdateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((BankBranchEntity?)null);

        var act = async () => await sut.UpdateAsync(42, request, cts.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró la sucursal solicitada.");
        _bankBranchRepository.Verify(repository => repository.UpdateAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFoundException_WhenBranchDoesNotExist()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((BankBranchEntity?)null);

        var act = async () => await sut.DeleteAsync(42, cts.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("No se encontró la sucursal solicitada.");
        _bankBranchRepository.Verify(repository => repository.DeleteAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.CreateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _bankBranchRepository.Verify(repository => repository.AddAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.UpdateAsync(42, null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _bankBranchRepository.Verify(repository => repository.UpdateAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _bankBranchRepository.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetByIdAsync(10, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _bankBranchRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new BankBranchCreateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.CreateAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _bankBranchRepository.Verify(repository => repository.AddAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new BankBranchUpdateRequestDto
        {
            Name = "Central",
            City = "Madrid"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.UpdateAsync(42, request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _bankBranchRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _bankBranchRepository.Verify(repository => repository.UpdateAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.DeleteAsync(42, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _bankBranchRepository.Verify(repository => repository.DeleteAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowDomainException_WhenRequestIsInvalid()
    {
        var sut = CreateSut();
        var branch = CreateBankBranch(21, "Central", "Madrid");
        var request = new BankBranchUpdateRequestDto
        {
            Name = " ",
            City = " "
        };
        var cts = new CancellationTokenSource();
        _bankBranchRepository
            .Setup(repository => repository.GetByIdAsync(branch.Id, cts.Token))
            .ReturnsAsync(branch);

        var act = async () => await sut.UpdateAsync(branch.Id, request, cts.Token);

        await act.Should().ThrowAsync<DomainException>();
        _bankBranchRepository.Verify(repository => repository.UpdateAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDomainException_WhenRequestIsInvalid()
    {
        var sut = CreateSut();
        var request = new BankBranchCreateRequestDto
        {
            Name = " ",
            City = " "
        };

        var act = async () => await sut.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        _bankBranchRepository.Verify(repository => repository.AddAsync(It.IsAny<BankBranchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BankBranchEntity CreateBankBranch(int id, string name, string city)
    {
        var branch = new BankBranchEntity(name, city);
        typeof(BankBranchEntity).GetProperty(nameof(BankBranchEntity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(branch, id);
        return branch;
    }
}


