using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController(IFundManagementService fundManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ClientBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientBalanceDto>> GetClientAsync(CancellationToken cancellationToken)
    {
        var client = await fundManagementService.GetClientAsync(cancellationToken);
        return Ok(client);
    }
}

