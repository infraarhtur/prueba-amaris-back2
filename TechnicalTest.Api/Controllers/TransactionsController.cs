using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(IFundManagementService fundManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        var transactions = await fundManagementService.GetTransactionsAsync(cancellationToken);
        return Ok(transactions);
    }
}

