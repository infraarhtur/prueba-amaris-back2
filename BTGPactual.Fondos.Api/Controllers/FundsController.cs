using BTGPactual.Fondos.Application.DTOs;
using BTGPactual.Fondos.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BTGPactual.Fondos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

