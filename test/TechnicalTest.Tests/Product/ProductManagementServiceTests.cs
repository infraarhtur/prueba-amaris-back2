using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Services;
using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;
using Xunit;
using DomainClient = TechnicalTest.Domain.Entities.Client;
using DomainProduct = TechnicalTest.Domain.Entities.Product;
using DomainSubscription = TechnicalTest.Domain.Entities.Subscription;

namespace TechnicalTest.Tests.Products;

public partial class ProductManagementServiceTests
{
    private readonly Mock<IClientRepository> _clientRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.Parse("2025-01-02T03:04:05Z"));

    private ProductManagementService CreateSut() =>
        new(
            _clientRepository.Object,
            _productRepository.Object,
            _subscriptionRepository.Object,
            _notificationService.Object,
            _timeProvider);

    private static DomainProduct CreateProduct(
        int id = 10,
        string name = "Acción Europa",
        decimal minimumAmount = 100m,
        ProductCategory category = ProductCategory.FPV)
        => new(id, name, minimumAmount, category);

    private static DomainClient CreateClient(
        Guid? id = null,
        decimal balance = 1_000m,
        NotificationChannel channel = NotificationChannel.Email)
    {
        var client = new DomainClient(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "Alice",
            "Smith",
            "Madrid",
            balance,
            channel);
        return client;
    }

    private static DomainSubscription CreateSubscription(Guid? id = null, int productId = 10, Guid? clientId = null, decimal amount = 100m, DateTimeOffset? createdOn = null)
    {
        return new DomainSubscription(
            id ?? Guid.NewGuid(),
            clientId ?? Guid.NewGuid(),
            productId,
            amount,
            createdOn?.UtcDateTime ?? DateTime.UtcNow);
    }

    private static IReadOnlyCollection<DomainProduct> CreateProductList(params DomainProduct[] products) =>
        products.Length == 0
            ? [CreateProduct(1, "Fondo Conservador", 50m, ProductCategory.FPV), CreateProduct(2, "Fondo Dinámico", 150m, ProductCategory.FIC)]
            : products.ToImmutableArray();

    private static IReadOnlyCollection<DomainSubscription> CreateSubscriptionList(params DomainSubscription[] subscriptions) =>
        subscriptions.Length == 0
            ? [CreateSubscription(amount: 150m), CreateSubscription(amount: 250m)]
            : subscriptions.ToImmutableArray();

    [Fact]
    public async Task GetProductsAsync_ShouldReturnMappedProducts()
    {
        var sut = CreateSut();
        var products = CreateProductList();
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetAllAsync(cts.Token))
            .ReturnsAsync(products);

        var result = await sut.GetProductsAsync(cts.Token);

        result.Should().BeEquivalentTo(products.Select(product => new ProductDto(product.Id, product.Name, product.MinimumAmount, product.Category.ToString())));
        _productRepository.Verify(repository => repository.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetProductsAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetProductsAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _productRepository.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnMappedProduct()
    {
        var sut = CreateSut();
        var product = CreateProduct(7, "Bonos Globales", 200m, ProductCategory.FIC);
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, cts.Token))
            .ReturnsAsync(product);

        var result = await sut.GetProductByIdAsync(product.Id, cts.Token);

        AssertProductDto(result, product);
        _productRepository.Verify(repository => repository.GetByIdAsync(product.Id, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.GetProductByIdAsync(42, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No se encontró el producto con id 42.");
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetProductByIdAsync(5, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _productRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldAddProductAndReturnDto()
    {
        var sut = CreateSut();
        var request = new ProductCreateRequestDto(10, "ETF Tecnológico", 500m, ProductCategory.FPV.ToString());
        DomainProduct? addedProduct = null;
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.Id, cts.Token))
            .ReturnsAsync((DomainProduct?)null);
        _productRepository
            .Setup(repository => repository.AddAsync(It.IsAny<DomainProduct>(), cts.Token))
            .Callback<DomainProduct, CancellationToken>((product, _) => addedProduct = product)
            .Returns(Task.CompletedTask);

        var result = await sut.CreateProductAsync(request, cts.Token);

        addedProduct.Should().NotBeNull();
        addedProduct!.Id.Should().Be(request.Id);
        addedProduct.Name.Should().Be(request.Name);
        addedProduct.MinimumAmount.Should().Be(request.MinimumAmount);
        addedProduct.Category.Should().Be(ProductCategory.FPV);
        AssertProductDto(result, addedProduct);
        _productRepository.Verify(repository => repository.AddAsync(addedProduct, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowDomainException_WhenProductAlreadyExists()
    {
        var sut = CreateSut();
        var existing = CreateProduct(9);
        var request = new ProductCreateRequestDto(existing.Id, "Duplicado", 100m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);

        var act = async () => await sut.CreateProductAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"Ya existe un producto con id {existing.Id}.");
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowDomainException_WhenCategoryIsUnknown()
    {
        var sut = CreateSut();
        var request = new ProductCreateRequestDto(15, "Producto", 100m, "Desconocida");
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.Id, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.CreateProductAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Categoría de producto desconocida: Desconocida.");
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.CreateProductAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowDomainException_WhenNameIsInvalid()
    {
        var sut = CreateSut();
        var request = new ProductCreateRequestDto(20, " ", 100m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.Id, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.CreateProductAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("El nombre del producto es obligatorio.");
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowDomainException_WhenAmountIsInvalid()
    {
        var sut = CreateSut();
        var request = new ProductCreateRequestDto(21, "Producto", 0m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.Id, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.CreateProductAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("El monto mínimo del producto debe ser mayor a cero.");
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new ProductCreateRequestDto(22, "Producto", 100m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.CreateProductAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _productRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProductAndReturnDto()
    {
        var sut = CreateSut();
        var existing = CreateProduct(30, "Anterior", 100m, ProductCategory.FPV);
        var request = new ProductUpdateRequestDto("Actualizado", 250m, ProductCategory.FIC.ToString());
        DomainProduct? updatedProduct = null;
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);
        _productRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), cts.Token))
            .Callback<DomainProduct, CancellationToken>((product, _) => updatedProduct = product)
            .Returns(Task.CompletedTask);

        var result = await sut.UpdateProductAsync(existing.Id, request, cts.Token);

        updatedProduct.Should().NotBeNull();
        updatedProduct!.Id.Should().Be(existing.Id);
        updatedProduct.Name.Should().Be(request.Name);
        updatedProduct.MinimumAmount.Should().Be(request.MinimumAmount);
        updatedProduct.Category.Should().Be(ProductCategory.FIC);
        AssertProductDto(result, updatedProduct);
        _productRepository.Verify(repository => repository.UpdateAsync(updatedProduct, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var request = new ProductUpdateRequestDto("Nombre", 100m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.UpdateProductAsync(99, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No se encontró el producto con id 99.");
        _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.UpdateProductAsync(10, null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
        _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowDomainException_WhenCategoryIsUnknown()
    {
        var sut = CreateSut();
        var existing = CreateProduct(33);
        var request = new ProductUpdateRequestDto("Producto", 100m, "Desconocida");
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);

        var act = async () => await sut.UpdateProductAsync(existing.Id, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Categoría de producto desconocida: Desconocida.");
        _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowDomainException_WhenAmountIsInvalid()
    {
        var sut = CreateSut();
        var existing = CreateProduct(34);
        var request = new ProductUpdateRequestDto("Producto", 0m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);

        var act = async () => await sut.UpdateProductAsync(existing.Id, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("El monto mínimo del producto debe ser mayor a cero.");
        _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowDomainException_WhenNameIsInvalid()
    {
        var sut = CreateSut();
        var existing = CreateProduct(35);
        var request = new ProductUpdateRequestDto(" ", 100m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);

        var act = async () => await sut.UpdateProductAsync(existing.Id, request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("El nombre del producto es obligatorio.");
        _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new ProductUpdateRequestDto("Producto", 100m, ProductCategory.FPV.ToString());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.UpdateProductAsync(36, request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldRemoveProduct()
    {
        var sut = CreateSut();
        var existing = CreateProduct(40);
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(existing.Id, cts.Token))
            .ReturnsAsync(existing);
        _productRepository
            .Setup(repository => repository.DeleteAsync(existing, cts.Token))
            .Returns(Task.CompletedTask);

        await sut.DeleteProductAsync(existing.Id, cts.Token);

        _productRepository.Verify(repository => repository.DeleteAsync(existing, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.DeleteProductAsync(50, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No se encontró el producto con id 50.");
        _productRepository.Verify(repository => repository.DeleteAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.DeleteProductAsync(51, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _productRepository.Verify(repository => repository.DeleteAsync(It.IsAny<DomainProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetClientAsync_ShouldReturnMappedClientBalance()
    {
        var sut = CreateSut();
        var client = CreateClient();
        var cts = new CancellationTokenSource();
        _clientRepository
            .Setup(repository => repository.GetDefaultAsync(cts.Token))
            .ReturnsAsync(client);

        var result = await sut.GetClientAsync(cts.Token);

        result.Should().BeEquivalentTo(new ClientBalanceDto(client.Id, client.Balance, client.NotificationChannel.ToString()));
        _clientRepository.Verify(repository => repository.GetDefaultAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetClientAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetClientAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _clientRepository.Verify(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldReturnMappedSubscriptions()
    {
        var sut = CreateSut();
        var subscriptions = CreateSubscriptionList();
        var cts = new CancellationTokenSource();
        _subscriptionRepository
            .Setup(repository => repository.GetAllAsync(cts.Token))
            .ReturnsAsync(subscriptions);

        var result = await sut.GetSubscriptionsAsync(cts.Token);

        result.Should().BeEquivalentTo(subscriptions.Select(subscription => new SubscriptionDto(subscription.Id, subscription.ClientId, subscription.ProductId, subscription.Amount, subscription.SubscribedAtUtc, subscription.CancelledAtUtc, subscription.IsActive)));
        _subscriptionRepository.Verify(repository => repository.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.GetSubscriptionsAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _subscriptionRepository.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldCreateSubscriptionAndNotify()
    {
        var sut = CreateSut();
        var product = CreateProduct(60, "Producto Premium", 200m, ProductCategory.FPV);
        var client = CreateClient(balance: 500m, channel: NotificationChannel.Sms);
        var request = new SubscriptionRequestDto(product.Id, client.Id);
        DomainSubscription? addedSubscription = null;
        var cts = new CancellationTokenSource();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(client.Id, cts.Token))
            .ReturnsAsync(client);
        _clientRepository
            .Setup(repository => repository.UpdateAsync(client, cts.Token))
            .Returns(Task.CompletedTask);
        _subscriptionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), cts.Token))
            .Callback<DomainSubscription, CancellationToken>((subscription, _) => addedSubscription = subscription)
            .Returns(Task.CompletedTask);
        _notificationService
            .Setup(service => service.NotifyAsync(client, product, client.NotificationChannel, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.SubscribeAsync(request, cts.Token);

        client.Balance.Should().Be(300m);
        addedSubscription.Should().NotBeNull();
        addedSubscription!.ClientId.Should().Be(client.Id);
        addedSubscription.ProductId.Should().Be(product.Id);
        addedSubscription.Amount.Should().Be(product.MinimumAmount);
        addedSubscription.SubscribedAtUtc.Should().Be(nowUtc);
        result.Should().BeEquivalentTo(new SubscriptionDto(addedSubscription.Id, client.Id, product.Id, product.MinimumAmount, nowUtc, null, true));
        _clientRepository.Verify(repository => repository.UpdateAsync(client, cts.Token), Times.Once);
        _subscriptionRepository.Verify(repository => repository.AddAsync(addedSubscription, cts.Token), Times.Once);
        _notificationService.Verify(service => service.NotifyAsync(client, product, client.NotificationChannel, cts.Token), Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var request = new SubscriptionRequestDto(70, Guid.NewGuid());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(request.ProductId, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.SubscribeAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró el producto con id {request.ProductId}.");
        _subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(service => service.NotifyAsync(It.IsAny<DomainClient>(), It.IsAny<DomainProduct>(), It.IsAny<NotificationChannel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldThrowDomainException_WhenClientDoesNotExist()
    {
        var sut = CreateSut();
        var product = CreateProduct(80);
        var request = new SubscriptionRequestDto(product.Id, Guid.NewGuid());
        var cts = new CancellationTokenSource();
        _productRepository
            .Setup(repository => repository.GetByIdAsync(product.Id, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(request.ClientId, cts.Token))
            .ReturnsAsync((DomainClient?)null);

        var act = async () => await sut.SubscribeAsync(request, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró el cliente con id {request.ClientId}.");
        _subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(service => service.NotifyAsync(It.IsAny<DomainClient>(), It.IsAny<DomainProduct>(), It.IsAny<NotificationChannel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        var sut = CreateSut();

        var act = async () => await sut.SubscribeAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SubscribeAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var request = new SubscriptionRequestDto(90, Guid.NewGuid());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.SubscribeAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _subscriptionRepository.Verify(repository => repository.AddAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldCancelSubscriptionAndRefundClient()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription(productId: 100, amount: 150m);
        var product = CreateProduct(subscription.ProductId, "Fondo Premium", subscription.Amount, ProductCategory.FPV);
        var client = CreateClient(id: subscription.ClientId, balance: 400m);
        DomainSubscription? updatedSubscription = null;
        var cts = new CancellationTokenSource();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);
        _subscriptionRepository
            .Setup(repository => repository.UpdateAsync(subscription, cts.Token))
            .Callback<DomainSubscription, CancellationToken>((sub, _) => updatedSubscription = sub)
            .Returns(Task.CompletedTask);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ProductId, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ClientId, cts.Token))
            .ReturnsAsync(client);
        _clientRepository
            .Setup(repository => repository.UpdateAsync(client, cts.Token))
            .Returns(Task.CompletedTask);

        var result = await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        subscription.CancelledAtUtc.Should().Be(nowUtc);
        subscription.IsActive.Should().BeFalse();
        client.Balance.Should().Be(550m);
        updatedSubscription.Should().BeSameAs(subscription);
        result.Should().BeEquivalentTo(new SubscriptionDto(subscription.Id, subscription.ClientId, subscription.ProductId, subscription.Amount, subscription.SubscribedAtUtc, nowUtc, false));
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(subscription, cts.Token), Times.Once);
        _clientRepository.Verify(repository => repository.UpdateAsync(client, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldThrowDomainException_WhenSubscriptionDoesNotExist()
    {
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscriptionId, cts.Token))
            .ReturnsAsync((DomainSubscription?)null);

        var act = async () => await sut.CancelSubscriptionAsync(subscriptionId, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No se encontró la suscripción solicitada.");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldThrowDomainException_WhenSubscriptionAlreadyCancelled()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription();
        subscription.Cancel(DateTime.UtcNow);
        var cts = new CancellationTokenSource();
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);

        var act = async () => await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("La suscripción ya se encuentra cancelada.");
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldThrowDomainException_WhenProductDoesNotExist()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription(productId: 110);
        var cts = new CancellationTokenSource();
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ProductId, cts.Token))
            .ReturnsAsync((DomainProduct?)null);

        var act = async () => await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage($"No se encontró el producto con id {subscription.ProductId}.");
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldThrowDomainException_WhenClientDoesNotExist()
    {
        var sut = CreateSut();
        var subscription = CreateSubscription(productId: 120);
        var product = CreateProduct(subscription.ProductId);
        var cts = new CancellationTokenSource();
        _subscriptionRepository
            .Setup(repository => repository.GetByIdAsync(subscription.Id, cts.Token))
            .ReturnsAsync(subscription);
        _productRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ProductId, cts.Token))
            .ReturnsAsync(product);
        _clientRepository
            .Setup(repository => repository.GetByIdAsync(subscription.ClientId, cts.Token))
            .ReturnsAsync((DomainClient?)null);

        var act = async () => await sut.CancelSubscriptionAsync(subscription.Id, cts.Token);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("No se encontró el cliente asociado a la suscripción.");
        _subscriptionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainSubscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _clientRepository.Verify(repository => repository.UpdateAsync(It.IsAny<DomainClient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldThrowOperationCanceledException_WhenTokenCancelled()
    {
        var sut = CreateSut();
        var subscriptionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sut.CancelSubscriptionAsync(subscriptionId, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _subscriptionRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <remarks>
    /// Casos a cubrir por método:
    /// <list type="bullet">
    /// <item><c>GetProductsAsync</c>: retorno mapeado, token cancelado.</item>
    /// <item><c>GetProductByIdAsync</c>: producto encontrado, excepción por inexistente, token cancelado.</item>
    /// <item><c>CreateProductAsync</c>: alta exitosa, duplicado existente, categoría inválida, request nulo, token cancelado, validaciones de dominio de nombre/monto.</item>
    /// <item><c>UpdateProductAsync</c>: actualización exitosa, inexistente, request nulo, categoría inválida, token cancelado, validaciones de dominio.</item>
    /// <item><c>DeleteProductAsync</c>: eliminado existente, inexistente, token cancelado.</item>
    /// <item><c>GetClientAsync</c>: retorno mapeado, token cancelado.</item>
    /// <item><c>GetSubscriptionsAsync</c>: retorno mapeado, token cancelado.</item>
    /// <item><c>SubscribeAsync</c>: suscripción exitosa (debit, guardado, notificación, tiempo fijo), inexistencia de producto/cliente, request nulo, token cancelado.</item>
    /// <item><c>CancelSubscriptionAsync</c>: cancelación exitosa (estado, update, crédito), suscripción inexistente, ya cancelada, producto/cliente inexistente, token cancelado.</item>
    /// </list>
    /// </remarks>
    private static void AssertProductDto(ProductDto dto, DomainProduct product)
    {
        dto.Id.Should().Be(product.Id);
        dto.Name.Should().Be(product.Name);
        dto.MinimumAmount.Should().Be(product.MinimumAmount);
        dto.Category.Should().Be(product.Category.ToString());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}


