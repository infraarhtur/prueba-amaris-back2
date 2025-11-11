using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController(IProductManagementService productManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        var transactions = await productManagementService.GetTransactionsAsync(cancellationToken);
        return Ok(transactions);
    }
}

