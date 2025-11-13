using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IProductManagementService productManagementService) : ControllerBase
{
    private const string GetProductByIdRouteName = "GetProductById";

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await productManagementService.GetProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}", Name = GetProductByIdRouteName)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await productManagementService.GetProductByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProductAsync([FromBody] ProductCreateRequestDto request, CancellationToken cancellationToken)
    {
        var created = await productManagementService.CreateProductAsync(request, cancellationToken);
        return CreatedAtRoute(GetProductByIdRouteName, new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProductAsync(int id, [FromBody] ProductUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await productManagementService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        await productManagementService.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }
}


