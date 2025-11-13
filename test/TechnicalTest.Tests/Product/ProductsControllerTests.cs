using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalTest.Api.Controllers;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Xunit;

namespace TechnicalTest.Tests.Products;

public class ProductsControllerTests
{
    private readonly Mock<IProductManagementService> _productManagementService = new();

    private ProductsController CreateSut() => new(_productManagementService.Object);

    [Fact]
    public async Task GetProductsAsync_ShouldReturnOkWithProducts()
    {
        var sut = CreateSut();
        var expected =
            new List<ProductDto>
            {
                new(1, "Fondo Conservador", 50m, "FPV"),
                new(2, "Fondo Dinámico", 150m, "FIC")
            };
        var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.GetProductsAsync(cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetProductsAsync(cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        _productManagementService.Verify(service => service.GetProductsAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnOkWithProduct()
    {
        var sut = CreateSut();
        const int productId = 10;
        var expected = new ProductDto(productId, "ETF Tecnológico", 200m, "FPV");
        var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.GetProductByIdAsync(productId, cts.Token))
            .ReturnsAsync(expected);

        var result = await sut.GetProductByIdAsync(productId, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expected);
        _productManagementService.Verify(service => service.GetProductByIdAsync(productId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldReturnCreatedProduct()
    {
        var sut = CreateSut();
        var request = new ProductCreateRequestDto(25, "Bonos Globales", 300m, "FIC");
        var created = new ProductDto(request.Id, request.Name, request.MinimumAmount, request.Category);
        var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.CreateProductAsync(request, cts.Token))
            .ReturnsAsync(created);

        var result = await sut.CreateProductAsync(request, cts.Token);

        var createdResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        createdResult.RouteName.Should().Be("GetProductById");
        createdResult.RouteValues.Should().NotBeNull();
        createdResult.RouteValues!["id"].Should().Be(created.Id);
        createdResult.Value.Should().Be(created);
        _productManagementService.Verify(service => service.CreateProductAsync(request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnUpdatedProduct()
    {
        var sut = CreateSut();
        const int productId = 30;
        var request = new ProductUpdateRequestDto("Producto Actualizado", 400m, "FPV");
        var updated = new ProductDto(productId, request.Name, request.MinimumAmount, request.Category);
        var cts = new CancellationTokenSource();
        _productManagementService
            .Setup(service => service.UpdateProductAsync(productId, request, cts.Token))
            .ReturnsAsync(updated);

        var result = await sut.UpdateProductAsync(productId, request, cts.Token);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updated);
        _productManagementService.Verify(service => service.UpdateProductAsync(productId, request, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnNoContent()
    {
        var sut = CreateSut();
        const int productId = 40;
        var cts = new CancellationTokenSource();

        var result = await sut.DeleteProductAsync(productId, cts.Token);

        result.Should().BeOfType<NoContentResult>();
        _productManagementService.Verify(service => service.DeleteProductAsync(productId, cts.Token), Times.Once);
    }
}


