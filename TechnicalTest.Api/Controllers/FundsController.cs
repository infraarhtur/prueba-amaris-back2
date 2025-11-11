using Microsoft.AspNetCore.Authorization;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FundsController(IFundManagementService fundManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FundDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FundDto>>> GetFundsAsync(CancellationToken cancellationToken)
    {
        var funds = await fundManagementService.GetFundsAsync(cancellationToken);
        return Ok(funds);
    }
}

