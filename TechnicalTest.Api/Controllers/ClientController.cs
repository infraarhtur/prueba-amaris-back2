using System.Collections.Generic;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController(
    IFundManagementService fundManagementService,
    IClientService clientService) : ControllerBase
{
    private readonly IFundManagementService _fundManagementService = fundManagementService ?? throw new ArgumentNullException(nameof(fundManagementService));
    private readonly IClientService _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

    [HttpGet("balance")]
    [ProducesResponseType(typeof(ClientBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientBalanceDto>> GetDefaultClientBalanceAsync(CancellationToken cancellationToken)
    {
        var client = await _fundManagementService.GetClientAsync(cancellationToken).ConfigureAwait(false);
        return Ok(client);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClientDto>>> GetClientsAsync(CancellationToken cancellationToken)
    {
        var clients = await _clientService.GetAsync(cancellationToken).ConfigureAwait(false);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetClientByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(client);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientDto>> CreateClientAsync([FromBody] ClientCreateRequestDto request, CancellationToken cancellationToken)
    {
        var created = await _clientService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetClientByIdAsync), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> UpdateClientAsync(Guid id, [FromBody] ClientUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await _clientService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientAsync(Guid id, CancellationToken cancellationToken)
    {
        await _clientService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}

